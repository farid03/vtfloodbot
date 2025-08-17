module fp_lab4.Configuration.Configuration

open FSharp.Configuration
open System

type AppConfig = YamlConfig<"config.yaml">

let config = AppConfig()

let getFromEnvOrDefault (envParameter: string) (defaultValue: string) =
    Environment.GetEnvironmentVariable(envParameter)
    |> function
        | null -> defaultValue
        | x -> x

let telegramBotToken =
    getFromEnvOrDefault "TELEGRAM_BOT_TOKEN" config.Bot.TelegramToken

let chatId = getFromEnvOrDefault "CHAT_ID" (config.Bot.ChatId.ToString())

let adminUserId =
    getFromEnvOrDefault "ADMIN_USER_ID" (config.Bot.AdminUserId.ToString())

let myItmoUrl = config.Source.MyItmo.Url.OriginalString

let myItmoBearerToken =
    getFromEnvOrDefault "MY_ITMO_BEARER_TOKEN" config.Source.MyItmo.Bearer

let connectionString =
    getFromEnvOrDefault "DB_CONNECTION_STRING" config.DB.ConnectionString

let startLoad = config.Source.Start
let groups = config.Source.Groups
let groupLimit = config.Source.EachLimit
