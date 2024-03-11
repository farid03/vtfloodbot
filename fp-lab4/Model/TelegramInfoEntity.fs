module fp_lab4.Model.TelegramInfoEntity

[<AllowNullLiteral>]
type TelegramInfoEntity(student_id: int, tg_user_id: int64, username: string, exists: bool) =
    let mutable studentIdValue = student_id
    let mutable usernameValue = username
    let mutable existsValue = exists
    let mutable tgUserIdValue = tg_user_id

    member this.student_id
        with get () = studentIdValue
        and set (value) = studentIdValue <- value

    member this.username
        with get () = usernameValue
        and set (value) = usernameValue <- value

    member this.exists
        with get () = existsValue
        and set (value) = existsValue <- value

    member this.tg_user_id
        with get () = tgUserIdValue
        and set (value) = tgUserIdValue <- value
