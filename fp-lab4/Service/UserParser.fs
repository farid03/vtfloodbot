module fp_lab4.Service.UserParser

open System.Text.Json
open FsHttp
open fp_lab4.Configuration
open fp_lab4.Logger.Logger
open fp_lab4.Model.Student

let private requestGroup group limit offset =
    http {
        GET Configuration.myItmoUrl
        query [ "limit", limit.ToString(); "offset", offset.ToString(); "q", group ]

        AuthorizationBearer Configuration.myItmoBearerToken

        header "accept-language" "ru"
    }
    |> Request.send
    |> Response.toJson

let private toStudent (json: JsonElement) =
    let student =
        { id = json.GetProperty("id").GetInt32()
          fio = json.GetProperty("fio").GetString()
          gender = json.GetProperty("gender").GetString()
          phone = json.GetProperty("phone").GetString()
          work = json.GetProperty("work").GetString()
          education = json.GetProperty("education").GetString()
          photo = json.GetProperty("photo").GetString() }

    student

let private getGroupStudents group limit =
    logInfo $"Getting students with limit={limit} for groupPattern: {group}"

    let rec fetchStudentsRecursively offset accumulatedStudents =
        let groupRequest = requestGroup group limit offset

        if (groupRequest.GetProperty("error_code").GetInt32() <> 0) then
            let errorMessage = groupRequest.GetProperty("error_message").GetString()
            logFatal errorMessage
            []
        else
            let studentsJson = groupRequest.GetProperty("result").GetProperty("data").GetList()

            if studentsJson.Length = 0 then
                accumulatedStudents
            else
                let newStudents = studentsJson |> List.map toStudent
                let updatedStudents = accumulatedStudents @ newStudents
                fetchStudentsRecursively (offset + studentsJson.Length) updatedStudents

    let students = fetchStudentsRecursively 0 []

    logInfo $"{group} students count: {students.Length}"

    students

let getAllStudents =
    logInfo "Getting all students"

    let students =
        Configuration.groups
        |> Seq.map (fun group -> getGroupStudents group Configuration.groupLimit)
        |> Seq.collect id
        |> Seq.toList

    logInfo $"All students count: {students.Length}"

    students
