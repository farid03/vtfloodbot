module fp_lab4.Bot

open System
open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open Configuration.Logger
open Configuration.Configuration
open Processor


let replyToMessage (ctx: UpdateContext) (text: string) =
    Api.sendMessageReply ctx.Update.Message.Value.Chat.Id text ctx.Update.Message.Value.MessageId
    |> api ctx.Config
    |> Async.Ignore
    |> Async.Start

let handleHelp (ctx: UpdateContext) =
    let username = ctx.Update.Message.Value.From.Value.Username
    logDbg $"Help command from {username} received"

    replyToMessage ctx "/start - приветствие\n/help - список команд\n/isu <isu_id> - получить доступ к чату"

let handleStart (ctx: UpdateContext) =
    let username = ctx.Update.Message.Value.From.Value.Username
    logDbg $"Start command from {username} received"

    replyToMessage
        ctx
        "Этот бот поможет получить доступ в секретный чат студентов.\nВведите /isu <ваш_ИСУ> для получения ссылки на вступление в чат."

let createInviteLink (ctx: UpdateContext) =
    let invite =
        Req.CreateChatInviteLink.Make(
            chatId = chatId,
            expireDate = DateTimeOffset.Now.AddMinutes(120).ToUnixTimeSeconds(),
            name = ctx.Update.Message.Value.From.Value.Username.Value,
            createsJoinRequest = true
        )
        |> api ctx.Config
        |> Async.RunSynchronously

    (Result.toOption invite).Value.InviteLink


let handleIsu (ctx: UpdateContext) (studentId: int) =
    let user = StudentsRepository.getById studentId
    let username = ctx.Update.Message.Value.From.Value.Username

    match user with
    | null ->
        logDbg $"User {username} trying to register with invalid studentId: {studentId}"
        replyToMessage ctx "Пользователь не найден!"
    | _ ->
        let tgUserId = ctx.Update.Message.Value.From.Value.Id

        let result = processInvite tgUserId studentId username

        match result with
        | true ->
            let inviteLink = createInviteLink ctx
            logDbg $"User {username} valid, invite link created"
            replyToMessage ctx $"Ссылка для вступления: {inviteLink}.\nСсылка действительна 2 часа."
        | false ->
            logDbg $"User {username} already registered in chat"
            replyToMessage ctx "Пользователь уже зарегистрирован в чате!"

let handleLeaveChat (ctx: UpdateContext) =
    match ctx.Update.Message with
    | Some { LeftChatMember = leftChatMember } ->
        logDbg $"Leave from user with username:{leftChatMember.Value.Username}, uid:{leftChatMember.Value.Id}"
        processLeave leftChatMember.Value.Id
    | None -> logErr $"Error: no message in update {ctx.Update}"

let handleJoinChat (ctx: UpdateContext) =
    match ctx.Update.ChatJoinRequest with
    | Some { From = from } ->
        logDbg $"Join request from user with username: {from.Username}, uid:{from.Id}"
        let approve = processJoinRequest from.Id

        if approve then
            let result =
                Req.ApproveChatJoinRequest.Make(chatId = ctx.Update.ChatJoinRequest.Value.Chat.Id, userId = from.Id)
                |> api ctx.Config
                |> Async.RunSynchronously

            logDbg $"Join request has been approved: {approve}, result: {Result.toOption result}"
        else
            logDbg "Join request was not approved"

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
