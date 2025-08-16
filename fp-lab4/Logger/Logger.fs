module fp_lab4.Logger.Logger

open FSLogger
open TelegramNotifier

let private logger = Logger.ColorConsole |> Logger.withPath "app"

let logDbg (str: string) = logger.D str

let logInfo (str: string) =
    logger.I str
    sendLogToAdmin "INFO" str

let logWarn (str: string) =
    logger.W str
    sendLogToAdmin "WARNING" str

let logErr (msg: string) =
    logger.E msg
    sendLogToAdmin "ERROR" msg

let logFatal (msg: string) =
    logger.F msg
    sendLogToAdmin "FATAL" msg
