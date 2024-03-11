module fp_lab4.Service

open System.Text.Json
open FsHttp
open Configuration.Configuration
open Configuration.Logger
open Model.Student

let requestGroup group limit =
    http {
        GET myItmoUrl
        query [ "limit", limit.ToString(); "offset", "0"; "q", group ]

        AuthorizationBearer myItmoBearerToken

        header "accept-language" "ru"
    }
    |> Request.send
    |> Response.toJson

let toStudent (json: JsonElement) =
    let student =
        { id = json.GetProperty("id").GetInt32()
          fio = json.GetProperty("fio").GetString()
          gender = json.GetProperty("gender").GetString()
          phone = json.GetProperty("phone").GetString()
          work = json.GetProperty("work").GetString()
          education = json.GetProperty("education").GetString()
          photo = json.GetProperty("photo").GetString() }

    student

let getGroupStudents group limit =
    logInfo $"Getting students with limit={limit} for group: {group}"
    let groupRequest = requestGroup group limit

    if (groupRequest.GetProperty("error_code").GetInt32() <> 0) then
        failwith (groupRequest.GetProperty("error_message").GetString())

    let studentsJson = groupRequest.GetProperty("result").GetProperty("data").GetList()

    let students = studentsJson |> List.map toStudent
    logDbg $"{group} students count: {students.Length}"

    students

let getAllStudents =
    logInfo "Getting all students"

    let students =
        groups
        |> Seq.map (fun group -> getGroupStudents group groupLimit)
        |> Seq.collect id
        |> Seq.toList

    logDbg $"Students count: {students.Length}"

    students
