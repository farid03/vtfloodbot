module fp_lab4.Program

open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open Processor
open Configuration.Logger
open Configuration.Configuration


module Main =

    [<EntryPoint>]
    let main _ =
        logInfo $"Run configuration:\n{config}"

        if startLoad then
            logInfo "Load is started"
            saveAllStudents
        else
            logInfo "Load is skipped"

        logDbg "Start bot"

        async {
            let config =
                { Config.defaultConfig with
                    Token = telegramBotToken }

            let! _ = Api.deleteWebhookBase () |> api config
            return! startBot config Bot.updateArrived None
        }
        |> Async.RunSynchronously

        0
