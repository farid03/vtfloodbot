-- auto-generated definition
create table public.students
(
    id        integer not null
        constraint id
            primary key,
    fio       varchar,
    gender    varchar,
    phone     varchar,
    work      varchar,
    education varchar,
    photo     varchar,
    "group"   varchar
);

alter table public.students
    owner to postgres;

create table public.telegram_info
(
    student_id integer
        constraint student_id
            references public.students,
    username   varchar,
    exists     boolean,
    tg_user_id bigint not null
        constraint telegam_info_pk
            primary key
);

alter table public.telegram_info
    owner to postgres;
