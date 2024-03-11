# vtfloodbot

Телеграм-бот для допуска студентов в чат по их номеру студенческого билета.

## Запуск

Для запуска бота необходимо подготовить [файл конфигурации](#конфигурация), собрать образ приложения и запустить контейнер с помощью команд:

```bash
docker build -f fp-lab4/Dockerfile -t fp-lab4 .  
docker compose --profile application -f ./docker-compose/docker-compose.yaml up -d --wait --quiet-pull
```

## Использование
Пользователь может получить доступ к чату, отправив боту номер своего студенческого билета. 
Если студент с таким номером найден и не вступал в чат, то он получит ссылку на вступление в чат для подачи заявки. 
После подачи заявки бот одобриз заявку на вступление в чат для пользователя, который запрашивал доступ. 
В противном случае, бот сообщит об ошибке и пользователь не получит ссылку.

### Команды
```
/start - приветствие
/help - список команд
/isu <isu_id> - получить доступ к чату
```
### Конфигурация
Для работы бота необходимо создать файл `config.yaml` в корне проекта с содержимым:

```yaml
Bot:
  TelegramToken: "<telegram bot token>" # токен бота, подробнее https://core.telegram.org/bots#how-do-i-create-a-bot
  ChatId: -4116445966 # id чата, в который будут добавляться студенты

DB:
  ConnectionString: "Server=127.0.0.1;Port=5432;Database=postgres;User Id=postgres;Password=postgres;"
  NumberOfDeadlockRepeats: 5
  DefaultTimeout: 00:05:00

Source:
  MyItmo:
    Url: "https://my.itmo.ru/api/personalities/persons"
    Bearer: "<bearer token>"
  
  Start: false # true - заполнить базу при старте приложения, false - не заполнять
  EachLimit: 1000 # лимит запрашиваемых записей за один запрос обращения к источнику
  Groups: [ # список групп, которые будут добавлены в базу
    P3100,
    ]
```

## Реализация

### Парсинг данных
Для получения данных о студентах используется запрос к API [my.itmo.ru](https://my.itmo.ru/api/personalities/persons) с использованием токена авторизации.
Полученные данные в формате JSON преобразуются в объекты F#.

```fsharp
let requestGroup group limit =
    http {
        GET Configuration.myItmoUrl
        query [ "limit", limit.ToString(); "offset", "0"; "q", group ]

        AuthorizationBearer Configuration.myItmoBearerToken

        header "accept-language" "ru"
    }
    |> Request.send
    |> Response.toJson

...

let getGroupStudents group limit =
    logInfo $"Getting students with limit={limit} for group: {group}"
    let groupRequest = requestGroup group limit

    if (groupRequest.GetProperty("error_code").GetInt32() <> 0) then
        failwith (groupRequest.GetProperty("error_message").GetString())

    let studentsJson = groupRequest.GetProperty("result").GetProperty("data").GetList()

    let students = studentsJson |> List.map toStudent
    logDbg $"{group} students count: {students.Length}"

    students
    
...
```
Подробнее в [UserParser.fs](fp-lab4/Service/UserParser.fs)

### Bot API
Для работы с Telegram Bot API используется библиотека [Funogram](https://github.com/Dolfik1/Funogram).
Обработка команд происходит в функции `updateArrived`:
```fsharp
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
```
Для каждой команды определена своя функция обработки.
Подробнее в [Bot.fs](fp-lab4/Service/Bot.fs)

### Персистентность
Для хранения данных о студентах используется PostgreSQL. 
Для работы с базой данных используется библиотека EntityFramework.

В БД хранятся данные о студентах, которые запрашивали доступ к чату, и их статусе (одобрено/не одобрено).
Для этого есть две разные таблицы: `students` и `telegram_info`.

Схема базы данных указана в файле [init.sql](docker-compose/postgres/init.sql).

Пример кода для работы с БД на F#, табличка `students`:

DataContext:
```fsharp
type public StudentDataContext() =
    inherit DbContext()

    [<DefaultValue>]
    val mutable students: DbSet<StudentEntity>

    member public this.Student
        with get () = this.students
        and set p = this.students <- p

    override __.OnConfiguring(optionsBuilder: DbContextOptionsBuilder) =
        optionsBuilder.UseNpgsql(Configuration.connectionString) |> ignore

    override __.OnModelCreating(modelBuilder: ModelBuilder) =

        modelBuilder.Entity<StudentEntity>().ToTable("students") |> ignore

        modelBuilder.Entity<StudentEntity>().Property<int>("id") |> ignore
        modelBuilder.Entity<StudentEntity>().HasKey("id") |> ignore

let private ctx = new StudentDataContext()

let getById (id: int) = ctx.Student.Find(id)

...

let save (student: StudentEntity) =
    ctx.Student.Add(student) |> ignore
    ctx.SaveChanges() |> ignore
```

Entity:
```fsharp
module fp_lab4.StudentEntity

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
    
    ...

    new() = StudentEntity(0, "", "", "", "", "", "")

    new(student: Model.Student) =
        StudentEntity(
            student.id,
            student.fio,
            student.gender,
            student.phone,
            student.work,
            student.education,
            student.photo
        )
```
Подробнее можно ознакомиться c реализацией в директориях [Storage.fs](fp-lab4/Service/) и [Model](fp-lab4/Model/).