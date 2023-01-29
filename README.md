# TodoApp

## How to run

### Run published version using docker and docker-compose
#
Easiest way to get the app running is to use the included Dockerfile and docker-compose.

First you'll have to navigate to the root of this project on your command line and execute
```
docker build -t todoapp .
```
This will build the app and create todoapp image that it later used by docker-compose.

Next you will need a .env file for the docker-compose. This includes the required environmental variables for both the TodoApp and MySql database that is used with it. You can find a template on the root of this project called `.env.template`. Copy it to create the .env file. The .env file has a migrate variable that needs to be true for the first  time you start the application to apply the database migrations. On later startups you can change it to false or leave it true if you want it to automatically apply new migrations.

Other variables can be left untouched if you are simply just testing this out but otherwise it is recommended to change the password. Please note that the ConnectionStrings__MySqlDatabase variables have to match the MYSQL_* variables.

Now that the .env is ready you can run `docker-compose up` at the root of the project and after some seconds the app should be available at localhost:5000

### Run using dotnet run
#
Before starting the  app you will still need the database and set the environmental variables for the app via dotenv. But first you'll need the database. Easiest way to setup the database is to use docker. You can use this command for example:
```
docker run --name todomysql -e MYSQL_ROOT_PASSWORD=supersecretphrase -e MYSQL_PASSWORD=anothersecretphrase -e MYSQL_USER=todouser -e MYSQL_DATABASE=todoapp -p 3306:3306 -d  mysql
```
The example command uses same credentials as is found in to `TodoApp/.env.dev.template`. If you want to use the same credentials you can simply copy the `.env.dev.template` to `.env` (Note, that this time the .env should be inside the TodoApp directory).

Now you are ready to apply the migrations. You can either automatically do that on startupp by having `migrate=true` in the `.env` file but the recommended way is to apply it manually using `dotnet-ef database update` while inside the TodoApp directory.

Now you should be ready to run it with `dotnet run`. The app should be available at localhost:5001.
#
## How to use the app

The app shows all top level tasks in a single column. To create a new task click the "New" button on the top right corner. This will open a modal to create a task. To create a subtask just insert the parent id to the Parent Id field.

Sub tasks are lazy loaded every time a task is opened the first time. You can open a task by clicking it.
Open in the task shows the information of the task. Allows you to change the position of it by selecting the new index and clicking the "move" button. "Update" button simply opens similar modal as the create modal where you can change the details of the task or even change the Parent task. And Delete task deletes the task (and all subtasks with it).

You can change the order of the tasks from the navbar and apply it by clicking "Search". Note that the the move functionality is a bit buggy here if you have sorted it be any other order than Position Ascending. See Known issues/problems.

#
## Design

The application uses controller, service and repository pattern (mysql in this implementation) to save and fetch the tasks. Controller does the initial validation and service does the more complicated validation (such as making sure user can not create cyclical relationship with tasks). Validation errors detected in the service throw a custom exception which is handled by the CustomExceptionHandlingMiddleware to provide better error messages for user.

Tasks are saved in a mysql database where each task has a Foreign Key to another task. Null means the task is a top level task. This is how unlimited number and depth of subtasks are saved. 

User defined order is accomplished by having a Position column. This is 64bit unsigned int. By default each new task is current max position + uint32.maxValue. This leaves room for when user wants to move tasks around. A "rebalancing" is triggered when user wants to move task between two other tasks where there are no room left. This updates the task positions so that each task is again uint32.maxValue apart.

You can find the API documentation from the /swagger endpoint. For example `localhost:[port]/swagger`

This also comes with set of unit tests for both the TasksController and TodoTaskService in a different project (`TodoApp.UnitTests`) and Integration tests (`TodoApp.IntegrationTests`). Integration tests use testcontainers library to start a new mysql container before tests so docker is required to run tests.

#
## Known Issues/bugs
  - Database does not enforce unique ParentId - Position pair when ParentId is null (top level task). This is because every null in sql is different, hence every parentId Position pair is unique where ParentId is null. Current idea to fix this is to create lists where each task resides (Similar to Trello). That way there could be two unique indexes (ParentId - Position and ListId - Position).
  - Changing Task position when sorting by something else than Position ascending technically works (changes the position correctly), but the initial value for it in the app frontend is wrong, because it takes the indexOf position from the list. This could be fixed by sorting it locally and taking indexOf but that would not work with paginated responses where not every task is available at frontend.
  - The pagination could get inefficient with very high task count because it uses the "traditional" `LIMIT offset,pagesize` which forces the database to load the skipped rows also. This could be improved by using WHERE filter instead of offset. For example when sorting by ascending position the next page could be `WHERE Position > lastPosition LIMIT pagesize`
