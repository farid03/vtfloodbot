module fp_lab4.Service.EventProcessor

open fp_lab4.Logger.Logger
open fp_lab4.Model.TelegramInfoEntity
open fp_lab4.Storage

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

    tgUserExists, studentExists

let rollbackInvite (tgUserId: int64) (studentId: int) (username: string option) : bool =
    logErr $"Rollback processed invite for tgUserId:{tgUserId}, studentId:{studentId}, username:{username}"
    TelegramInfoRepository.deleteByTgUserId tgUserId

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
