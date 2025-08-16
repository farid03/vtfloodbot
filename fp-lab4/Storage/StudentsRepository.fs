module fp_lab4.Storage.StudentsRepository

open Microsoft.EntityFrameworkCore
open fp_lab4.Model.StudentEntity
open fp_lab4.Configuration.Configuration

type public StudentDataContext() =
    inherit DbContext()

    [<DefaultValue>]
    val mutable students: DbSet<StudentEntity>

    member public this.Student
        with get () = this.students
        and set p = this.students <- p

    override __.OnConfiguring(optionsBuilder: DbContextOptionsBuilder) =
        optionsBuilder.UseNpgsql(connectionString) |> ignore

    override __.OnModelCreating(modelBuilder: ModelBuilder) =

        modelBuilder.Entity<StudentEntity>().ToTable("students") |> ignore

        modelBuilder.Entity<StudentEntity>().Property<int>("id") |> ignore
        modelBuilder.Entity<StudentEntity>().HasKey("id") |> ignore

let private ctx = new StudentDataContext()

let getById (id: int) = ctx.Student.Find(id)

let contains (studentId: int) =
    ctx.Student.AnyAsync(fun s -> s.id = studentId)
    |> Async.AwaitTask
    |> Async.RunSynchronously

let upsert (student: StudentEntity) =
    if not (contains student.id) then
        ctx.Student.Add(student) |> ignore
    else
        ctx.Student.Update(student) |> ignore

    ctx.SaveChanges() |> ignore

let getCount () =
    async {
        let! count = ctx.Student.CountAsync() |> Async.AwaitTask
        return count
    }
    |> Async.RunSynchronously
