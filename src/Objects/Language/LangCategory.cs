namespace Ares.Objects.Language;

public class LangCategory
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Code { get; set; }

    public LangCategory(string name, string description, string code)
    {
        this.Name = name;
        this.Description = description;
        this.Code = code;
    }
}
