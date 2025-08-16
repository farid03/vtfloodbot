module fp_lab4.Service.DataProcessor

open fp_lab4
open fp_lab4.Model.StudentEntity
open fp_lab4.Logger.Logger
open fp_lab4.Service.UserParser
open fp_lab4.Storage

let private saveGroupOfStudents (students: Model.Student.Student list) =
    logInfo $"Start saving students. Known students count: {StudentsRepository.getCount ()}"

    let addGroup (entity: StudentEntity) =
        let educationInfo = entity.education.Split ','
        entity.group <- educationInfo[2]
        entity

    students
    |> List.map (fun student -> StudentEntity(student) |> addGroup)
    |> List.iter StudentsRepository.upsert

    logInfo $"Students saved: Actual count: {StudentsRepository.getCount ()}"

let saveAllStudents =
    logInfo "Start saving all students"

    getAllStudents |> saveGroupOfStudents
