module fp_lab4.TelegramInfoRepository

open Microsoft.EntityFrameworkCore
open Model.TelegramInfoEntity
open Configuration.Configuration

type public StudentDataContext() =
    inherit DbContext()

    [<DefaultValue>]
    val mutable students: DbSet<TelegramInfoEntity>

    member public this.TelegramInfo
        with get () = this.students
        and set p = this.students <- p

    override __.OnConfiguring(optionsBuilder: DbContextOptionsBuilder) =
        optionsBuilder.UseNpgsql(connectionString) |> ignore

    override __.OnModelCreating(modelBuilder: ModelBuilder) =

        modelBuilder.Entity<TelegramInfoEntity>().ToTable("telegram_info") |> ignore

        modelBuilder.Entity<TelegramInfoEntity>().Property<int64>("tg_user_id")
        |> ignore

        modelBuilder.Entity<TelegramInfoEntity>().HasKey("tg_user_id") |> ignore

let private ctx = new StudentDataContext()

let getById (id: int64) = ctx.TelegramInfo.Find(id)

let containsStudentId (studentId: int) =
    ctx.TelegramInfo.AnyAsync(fun s -> (s.student_id = studentId))
    |> Async.AwaitTask
    |> Async.RunSynchronously

let existsTgUserId (tgUserId: int64) =
    ctx.TelegramInfo.AnyAsync(fun s -> (s.tg_user_id = tgUserId))
    |> Async.AwaitTask
    |> Async.RunSynchronously

let save (tgEntity: TelegramInfoEntity) =
    ctx.TelegramInfo.Add(tgEntity) |> ignore
    ctx.SaveChanges() |> ignore

let update (tgEntity: TelegramInfoEntity) =
    ctx.TelegramInfo.Update(tgEntity) |> ignore
    ctx.SaveChanges() |> ignore

let delete (tgEntity: TelegramInfoEntity) =
    ctx.TelegramInfo.Remove(tgEntity) |> ignore
    ctx.SaveChanges() |> ignore
