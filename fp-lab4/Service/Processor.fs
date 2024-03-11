module fp_lab4.Processor

open Model.StudentEntity
open Service
open Configuration.Configuration
open Configuration.Logger
open Model.TelegramInfoEntity

let saveGroupOfStudents (group: string, students: Model.Student.Student list) =
    logDbg $"Start saving group {group}"

    let addGroup (entity: StudentEntity) =
        entity.group <- group
        entity

    students
    |> List.map (fun student -> StudentEntity(student) |> addGroup)
    |> List.iter StudentsRepository.save

    logDbg $"Saved group {group}, students count: {students.Length}"

let saveAllStudents =
    logInfo "Start saving all students"

    groups
    |> Seq.iter (fun group -> (group, (getGroupStudents group groupLimit)) |> saveGroupOfStudents)


let processInvite (tgUserId: int64) (studentId: int) (username: string option) =
    let tgUserExists = TelegramInfoRepository.existsTgUserId tgUserId
    let studentExists = TelegramInfoRepository.containsStudentId studentId

    match (tgUserExists, studentExists) with
    | true, _ -> logWarn $"User with tgUserId {tgUserId} already exists"
    | _, true -> logWarn $"Student with id {studentId} already exists"
    | false, false ->
        let usernameValue = if username.IsSome then username.Value else null
        let tgInfo = TelegramInfoEntity(studentId, tgUserId, usernameValue, false)
        TelegramInfoRepository.save tgInfo

    not (tgUserExists || studentExists) // возможно стоит возвращать оба значения и менять сообщение в зависимости от результата

let processJoinRequest (tgUserId: int64) =
    let tgInfo = TelegramInfoRepository.getById tgUserId

    match tgInfo with
    | null ->
        logWarn $"User with tgUserId {tgUserId} not found in telegram_info"
        false
    | info ->
        info.exists <- true
        TelegramInfoRepository.update info
        true

let processLeave (tgUserId: int64) =
    let tgInfo = TelegramInfoRepository.getById tgUserId

    match tgInfo with
    | null -> logWarn $"User with tgUserId {tgUserId} not found in telegram_info"
    | info -> TelegramInfoRepository.delete info
