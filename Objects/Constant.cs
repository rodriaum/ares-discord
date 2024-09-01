using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_OpenAI.Objects
{
    internal class Constant
    {
        public static string OPENAI_PROMPT_REFUSED_EXTRA = "Your request was rejected as a result of our safety system. Image descriptions generated from your prompt may contain text that is not allowed by our safety system. If you believe this was done in error, your request may succeed if retried, or by adjusting your prompt.";
        public static string OPENAI_PROMPT_REFUSED = "Your request was rejected as a result of our safety system. Your prompt may contain text that is not allowed by our safety system.";

        public static string UNABLE_PERFORM_TASK = "Ops! Não foi possível executar essa tarefa porque ouve um erro interno.\nPeço perdão!";

        public static string UNABLE_GET_MEMBER = "Ops! Não foi possível encontrar as informações do seu perfil.";

    }
}
