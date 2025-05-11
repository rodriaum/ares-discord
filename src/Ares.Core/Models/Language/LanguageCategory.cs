/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

namespace Ares.Core.Models.Language;

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
