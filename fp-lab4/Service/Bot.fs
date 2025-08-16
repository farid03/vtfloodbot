module fp_lab4.Service.Bot

open System
open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open Microsoft.FSharp.Core
open EventProcessor
open fp_lab4.Logger.Logger
open fp_lab4.Configuration
open fp_lab4.Storage

let private replyToMessage (ctx: UpdateContext) (text: string) =
    Api.sendMessageReply ctx.Update.Message.Value.Chat.Id text ctx.Update.Message.Value.MessageId
    |> api ctx.Config
    |> Async.Ignore
    |> Async.Start

let private handleHelp (ctx: UpdateContext) =
    let username = ctx.Update.Message.Value.From.Value.Username
    logDbg $"Help command from {username} received"

    replyToMessage ctx "/start - приветствие\n/help - список команд\n/isu <isu_id> - получить доступ к чату"

let private handleStart (ctx: UpdateContext) =
    let username = ctx.Update.Message.Value.From.Value.Username
    logDbg $"Start command from {username} received"

    replyToMessage
        ctx
        "Этот бот поможет получить доступ в секретный чат студентов.\nВведите /isu <ваш_ИСУ> для получения ссылки на вступление в чат."

let private createInviteLink (ctx: UpdateContext) =
    let username =
        match ctx.Update.Message with
        | Some message when message.From.IsSome && message.From.Value.Username.IsSome ->
            message.From.Value.Username.Value
        | _ -> "Unknown User"

    try
        let invite =
            Req.CreateChatInviteLink.Make(
                chatId = Configuration.chatId,
                expireDate = DateTimeOffset.Now.AddMinutes(120).ToUnixTimeSeconds(),
                name = username,
                createsJoinRequest = true
            )
            |> api ctx.Config
            |> Async.RunSynchronously

        match Result.toOption invite with
        | Some inviteResult -> inviteResult.InviteLink
        | None ->
            logErr "Failed to create invite link - API returned error"
            ""
    with ex ->
        logErr $"Exception creating invite link: {ex.Message}"
        ""

let private handleIsu (ctx: UpdateContext) (studentId: int) =
    let user = StudentsRepository.getById studentId

    match ctx.Update.Message with
    | Some message when message.From.IsSome ->
        let from = message.From.Value
        let usernameOpt = from.Username
        let tgUserId = from.Id

        match user with
        | null ->
            logInfo $"User {usernameOpt} trying to register with invalid studentId: {studentId}"
            replyToMessage ctx "Студент не найден!"
        | _ ->
            let tgUserExists, studentExists = processInvite tgUserId studentId usernameOpt

            if not (tgUserExists || studentExists) then
                let inviteLink = createInviteLink ctx

                if inviteLink <> "" then
                    logInfo $"User {usernameOpt} valid, invite link created for student {studentId}"
                    replyToMessage ctx $"Ссылка для вступления: {inviteLink}.\nСсылка действительна 2 часа."
                else
                    rollbackInvite tgUserId studentId usernameOpt |> ignore
                    logErr "Failed to create invite link for valid user"
                    replyToMessage ctx "Ошибка создания ссылки приглашения! Обратитесь к администратору."
            elif studentExists then
                logInfo $"Student {studentId} already registered in chat"
                replyToMessage ctx "Студент уже зарегистрирован в чате!"
            elif tgUserExists then
                logInfo $"User {usernameOpt} already registered in chat"
                replyToMessage ctx "Вы уже зарегистрированы в чате!"
            else
                logErr $"Invalid user registration state: username:{usernameOpt} studentId:{studentId}"
                replyToMessage ctx "Ошибка регистрации! Обратитесь к администратору."
    | _ ->
        logErr "Cannot process ISU command: no message or user data in update"
        replyToMessage ctx "Ошибка обработки команды!"

let private handleLeaveChat (ctx: UpdateContext) =
    match ctx.Update.Message with
    | Some { LeftChatMember = leftChatMember } ->
        logInfo $"Leave from user with username:{leftChatMember.Value.Username}, uid:{leftChatMember.Value.Id}"
        processLeave leftChatMember.Value.Id
    | None -> logErr $"Error: no message in update {ctx.Update}"

let private handleJoinChat (ctx: UpdateContext) =
    match ctx.Update.ChatJoinRequest with
    | Some { From = from } ->
        logInfo $"Join request from user with username: {from.Username}, uid:{from.Id}"
        let approve = processJoinRequest from.Id

        if approve then
            let result =
                Req.ApproveChatJoinRequest.Make(chatId = ctx.Update.ChatJoinRequest.Value.Chat.Id, userId = from.Id)
                |> api ctx.Config
                |> Async.RunSynchronously

            logInfo $"Join request from {from.Username} has been approved: {approve}, result: {Result.toOption result}"
        else
            logInfo $"Join request from {from.Username} was not approved"

    | None -> logErr $"Error: no message in update {ctx.Update}"

let updateArrived (ctx: UpdateContext) =
    let result =
        processCommands
            ctx
            [| cmd "/start" handleStart
               cmd "/help" handleHelp
               cmdScan "/isu %i" (fun isuId _ -> handleIsu ctx isuId)
               cmdScan "/isu%s" (fun _ _ -> replyToMessage ctx "Неправильный формат команды!\nВведите /isu <ваш_ИСУ>") |]

    if result then
        if ctx.Update.Message.IsSome && ctx.Update.Message.Value.LeftChatMember.IsSome then
            handleLeaveChat ctx
        else if ctx.Update.ChatJoinRequest.IsSome then
            handleJoinChat ctx
