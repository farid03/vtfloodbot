module fp_lab4.Program

open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open fp_lab4.Configuration
open fp_lab4.Logger.Logger
open fp_lab4.Service


module Main =

    [<EntryPoint>]
    let main _ =
        logInfo $"Run configuration:\n{Configuration.config}"

        if Configuration.startLoad then
            logInfo "Load is started"
            DataProcessor.saveAllStudents
        else
            logInfo "Load is skipped"

        logInfo "Start bot"

        async {
            let config =
                { Config.defaultConfig with
                    Token = Configuration.telegramBotToken }

            let! _ = Api.deleteWebhookBase () |> api config
            return! startBot config Bot.updateArrived None
        }
        |> Async.RunSynchronously

        0
