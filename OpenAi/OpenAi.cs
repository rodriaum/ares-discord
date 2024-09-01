using Discord_OpenAI.Objects.OpenAI;
using Discord_OpenAI.Objects.OpenAI.Model.Category;

namespace Discord_OpenAI
{
    internal class OpenAi
    {
        public static List<OpenAiModel> OpenAiModels = new List<OpenAiModel>();

        /* VARIABLE HELPER */

        public static List<OpenAiModel> GetOpenAiModelsByModelType(OpenAiModelCategory openAiModelCategory)
        {
            return OpenAiModels
                .Where(model => model.OpenAiModelCategory.Equals(openAiModelCategory))
                .ToList();
        }

        public static OpenAiModel GetOpenAiModelByMotorModelName(String model)
        {
            return OpenAiModels
                    .Where(model => model.Model.Equals(model))
                    .First();
        }


    }
}