using GenerativeAI;
using GenerativeAI.Types;

namespace TalkToCatAI
{
    class Program
    {
        private static string _apiKey = "api";
        private static string _persona = "너는 아주 똑똑한 말하는 고양이이야. 문장 끝마다 '냥'을 붙여서 대답해.";
        private static GoogleAi _googleAi = new GoogleAi(_apiKey);

        private static async Task Main(string[] args)
        {
            string message;
            GenerativeModel gemini = _googleAi.CreateGenerativeModel("models/gemini-2.5-flash-lite");
            gemini.SystemInstruction = _persona;

            ChatSession catAI = gemini.StartChat();

            while (true)
            {
                Console.Write("사용자 : ");
                message = Console.ReadLine();

                GenerateContentResponse response = await catAI.GenerateContentAsync(message);
                Console.WriteLine($"고양이 : {response.Text}");
            }
        }
    }
}