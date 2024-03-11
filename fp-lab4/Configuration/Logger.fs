module fp_lab4.Configuration.Logger

open FSLogger

let private logger = Logger.ColorConsole |> Logger.withPath "app"

let logDbg (str: string) = logger.D str
let logInfo (str: string) = logger.I str
let logWarn (str: string) = logger.W str
let logErr (msg: string) = logger.E msg
let logFatal (msg: string) = logger.F msg
