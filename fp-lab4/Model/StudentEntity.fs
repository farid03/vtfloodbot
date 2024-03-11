module fp_lab4.Model.StudentEntity

[<AllowNullLiteral>]
type StudentEntity(id: int, fio: string, gender: string, phone: string, work: string, education: string, photo: string)
    =
    let mutable idValue = id
    let mutable groupValue = fio
    let mutable fioValue = fio
    let mutable genderValue = gender
    let mutable phoneValue = phone
    let mutable workValue = work
    let mutable educationValue = education
    let mutable photoValue = photo

    member this.id
        with get () = idValue
        and set (value) = idValue <- value

    member this.group
        with get () = groupValue
        and set (value) = groupValue <- value

    member this.fio
        with get () = fioValue
        and set (value) = fioValue <- value

    member this.gender
        with get () = genderValue
        and set (value) = genderValue <- value

    member this.phone
        with get () = phoneValue
        and set (value) = phoneValue <- value

    member this.work
        with get () = workValue
        and set (value) = workValue <- value

    member this.education
        with get () = educationValue
        and set (value) = educationValue <- value

    member this.photo
        with get () = photoValue
        and set (value) = photoValue <- value

    new() = StudentEntity(0, "", "", "", "", "", "")

    new(student: Student.Student) =
        StudentEntity(
            student.id,
            student.fio,
            student.gender,
            student.phone,
            student.work,
            student.education,
            student.photo
        )
