namespace OMP.LSWTSS.CApi1;

public class SrcBuilder
{
    private readonly System.Text.StringBuilder _stringBuilder;

    public int Indent { get; set; }

    public SrcBuilder()
    {
        _stringBuilder = new System.Text.StringBuilder();
    }

    public void Append()
    {
        _stringBuilder.Append('\n');
    }

    public void Append(string? value)
    {
        if (value == null)
        {
            return;
        }

        foreach (var line in value.Split("\n"))
        {
            if (line != "")
            {
                _stringBuilder.Append(new string(' ', Indent * 4));
                _stringBuilder.Append(line);
            }
            _stringBuilder.Append('\n');
        }
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}