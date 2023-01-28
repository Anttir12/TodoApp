namespace TodoApp.Dtos;

public class MoveTodoTaskDto
{
    public int newIndex { get; set; }

    public MoveTodoTaskDto(int newIndex)
    {
        this.newIndex = newIndex;
    }


}