namespace Ares.src.Objects.OpenAI.Error
{
    public class OpenAiError
    {
        public ErrorCode Code { get; set; }
        public string Overview { get; set; }
        public string Cause { get; set; }
        public string Solution { get; set; }
        public string Type { get; set; }

        public static OpenAiError? TryGetErrorByMessage(string message)
        {
            return Errors.FirstOrDefault(error =>
                message.Contains(error.Type) ||
                message.Contains(error.Overview) ||
                message.Contains(error.Cause) ||
                message.Contains(error.Solution));
        }

        private static readonly HashSet<OpenAiError> Errors = new()
        {
            { new OpenAiError
                {
                    Overview = "Autenticação Inválida",
                    Cause = "Autenticação inválida",
                    Solution = "Certifique-se de que a [chave da API](https://platform.openai.com/settings/organization/api-keys) e a organização solicitante estão corretas.",
                    Type = "authentication_error"
                }
            },
            { new OpenAiError
                {
                    Code = ErrorCode.IncorrectApiKey,
                    Overview = "Chave de API incorreta fornecida",
                    Cause = "A chave de API fornecida está incorreta.",
                    Solution = "Certifique-se de que a [chave da API](https://platform.openai.com/settings/organization/api-keys) está correta.",
                    Type = "invalid_api_key"
                }
            },
            { new OpenAiError
                {
                    Code = ErrorCode.NotMemberOfOrganization,
                    Overview = "Você deve ser membro de uma organização para usar a API",
                    Cause = "Sua conta não faz parte de uma organização.",
                    Solution = "Entre em contato para ser adicionado a uma nova organização ou peça ao gerente da sua [organização](https://platform.openai.com/settings/organization/members) para convidá-lo para uma organização.",
                    Type = "authorization_error"
                }
            },
            { new OpenAiError
                {
                    Code = ErrorCode.UnsupportedRegion,
                    Overview = "País, região ou território não suportado",
                    Cause = "Você está acessando a API de um país, região ou território não suportado.",
                    Solution = "Consulte esta [página](https://platform.openai.com/docs/supported-countries) para mais informações.",
                    Type = "region_error"
                }
            },
            { new OpenAiError
                {
                    Code = ErrorCode.RateLimitReached,
                    Overview = "Limite de taxa atingido para solicitações",
                    Cause = "Você está enviando solicitações muito rapidamente.",
                    Solution = "Diminua a frequência de suas solicitações. Leia o [guia](https://platform.openai.com/docs/guides/rate-limits) de limite de taxa.",
                    Type = "rate_limit_error"
                }
            },
            { new OpenAiError
                {
                    Code = ErrorCode.QuotaExceeded,
                    Overview = "Você excedeu sua cota atual, verifique seu plano e detalhes de faturamento",
                    Cause = "Você ficou sem créditos ou atingiu seu limite máximo mensal.",
                    Solution = "Compre mais créditos ou aprenda como aumentar seus limites.",
                    Type = "insufficient_quota"
                }
            },
            { new OpenAiError
                {
                    Code = ErrorCode.ServerError,
                    Overview = "O servidor encontrou um erro ao processar sua solicitação",
                    Cause = "Problema nos nossos servidores.",
                    Solution = "Tente novamente após um breve intervalo e entre em contato conosco se o problema persistir. Verifique a página de status.",
                    Type = "server_error"
                }
            },
            { new OpenAiError
                {
                    Code = ErrorCode.EngineOverloaded,
                    Overview = "O mecanismo está sobrecarregado, tente novamente mais tarde",
                    Cause = "Nossos servidores estão enfrentando alto tráfego.",
                    Solution = "Por favor, tente novamente após um breve intervalo.",
                    Type = "server_overload"
                }
            }
        };
    }
}