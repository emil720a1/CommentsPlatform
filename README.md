# CommentsHub

SPA-застосунок для створення, перегляду та обговорення коментарів із підтримкою вкладених відповідей.

Назва проєкту поки є робочою і може бути змінена пізніше.

## Технології

- ASP.NET Core Web API
- Clean Architecture
- Entity Framework Core
- Microsoft SQL Server
- React
- Docker

## Основні можливості

- створення коментарів;
- відповіді на коментарі;
- каскадне відображення відповідей;
- сортування та пагінація;
- валідація даних;
- захист від XSS та SQL Injection;
- прикріплення зображень і текстових файлів.

## Статус

Проєкт перебуває на етапі планування. Спочатку буде реалізовано backend, після цього — frontend на React.

## Планована структура

```text
src/
  CommentsHub.Domain/
  CommentsHub.Application/
  CommentsHub.Infrastructure/
  CommentsHub.Api/

tests/
  CommentsHub.Domain.Tests/
  CommentsHub.Application.Tests/

docs/
```

## Документація

- [Task Board](docs/task-board.md)
- [Database ER Diagram](outputs/database-erd.md)

## Локальний запуск

Інструкція із запуску буде додана після створення базової .NET solution та Docker-конфігурації.
