module fp_lab4.Logger.TelegramNotifier

open System
open Funogram.Api
open Funogram.Telegram
open Funogram.Types
open Funogram.Telegram.Bot
open fp_lab4.Configuration

let private botConfig =
    { Config.defaultConfig with
        Token = Configuration.telegramBotToken }

let sendMessageToAdmin (message: string) =
    async {
        try
            let formattedMessage = $"```log\n{message}\n```"

            let! result =
                Req.SendMessage.Make(
                    chatId = int64 Configuration.adminUserId,
                    text = formattedMessage,
                    parseMode = Types.ParseMode.Markdown
                )
                |> api botConfig

            match result with
            | Ok _ -> () // success
            | Error e -> printf $"Error sending message to admin: {e.Description}\n"
        with ex ->
            printf $"Exception sending message to admin: {ex.Message}\n"
    }
    |> Async.RunSynchronously

let sendLogToAdmin (logLevel: string) (message: string) =
    let timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff")
    let formattedMessage = $"[{logLevel}] {timestamp}\n{message}"
    sendMessageToAdmin formattedMessage
