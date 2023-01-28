namespace TodoApp.Exceptions;

public class TooManyTasksException : Exception
{
    public TooManyTasksException(string msg) : base(msg)
    { }
}