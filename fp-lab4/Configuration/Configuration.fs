module fp_lab4.Configuration.Configuration

open FSharp.Configuration

type AppConfig = YamlConfig<"config.yaml">

let config = AppConfig()

let telegramBotToken = config.Bot.TelegramToken
let chatId = config.Bot.ChatId
let adminUserId = config.Bot.AdminUserId

let myItmoUrl = config.Source.MyItmo.Url.OriginalString
let myItmoBearerToken = config.Source.MyItmo.Bearer
let connectionString = config.DB.ConnectionString

let startLoad = config.Source.Start
let groups = config.Source.Groups
let groupLimit = config.Source.EachLimit
