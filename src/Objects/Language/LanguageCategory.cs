namespace Ares.src.Objects.Language;

public class LanguageCategory
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Code { get; set; }

    public LanguageCategory(string name, string description, string code)
    {
        this.Name = name;
        this.Description = description;
        this.Code = code;
    }
}
