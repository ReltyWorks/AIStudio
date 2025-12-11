using GenerativeAI;
using GenerativeAI.Types;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Gomoku
{
    // 선행작업
    // 솔루션 탐색기 > 프로젝트이름 우클릭 > NuGet 패키지관리 > Google_GenerativeAI 검색 후 설치
    class Program
    {
        // https://aistudio.google.com/
        // 위 사이트 들어가면 버튼 하나 누르면 간단한 api키를 줌, 아래에 복사해서 넣으면 끝

        private static string _apiKey = "aaa";
        private static string _persona = @"너는 지금부터 나와 오목(Gomoku) 게임을 하는 AI야. 너는 '백돌(○)'이고 후공이야.
    
            [게임 규칙 및 환경]
            1. C# .net 환경에서 작성된 간단한 게임
            2. 보드 크기: 15x15
            3. 좌표 체계: 
               - X축(가로): a ~ o
               - Y축(세로): 01 ~ 15
    
            [필수 응답 규칙 - 매우 중요]
            1. 너는 오직 '좌표값 3글자'만 말해야 해. (예: b05, h12)
            2. Y좌표가 1~9일 경우, 반드시 앞에 '0'을 붙여서 두 자리로 맞춰야 해. (예: a5 -> a05 로 작성)
            3. 인사, 설명, 마침표 등 좌표 외의 텍스트는 절대로 포함하지 마. (포함 시 프로그램 오류 발생)
            4. 내가 '좌표'라고 말하면, 너의 응답이 규칙 1, 2, 3 중 하나를 어겼다는 뜻이야. 다시 좌표값을 대답해.
            5. 내가 '중복1'이라고 말하면, 네가 둔 자리에 이미 니가 둔 백돌이 있다는 뜻이니 빈 곳을 찾아 다시 좌표를 말해.
            6. 내가 '중복2'이라고 말하면, 네가 둔 자리에 이미 내가 둔 흑돌이 있다는 뜻이니 빈 곳을 찾아 다시 좌표를 말해.
            7. 우리 둘 다, 오류가 없다면, 서로 좌표값만 번갈아 가며 주고받을꺼야.
            8. 대각선이나 가로 혹은 세로로 자신의 돌을 연속으로 5개 두면 승리하는거야.
    
            자, 내가 흑돌(●)로 먼저 시작할게. 내 좌표를 받으면 너의 수를 둬.";

        private static GoogleAi _googleAi = new GoogleAi(_apiKey);

        private static string _pointNull = "┼ ";
        private static char _pointWhite = '○';
        private static char _pointBlack = '●';

        private static char[] _leftSide = new char[15]
            { '①', '②', '③', '④', '⑤',
          '⑥', '⑦', '⑧', '⑨', '⑩',
          '⑪', '⑫', '⑬', '⑭', '⑮' };

        private static int[,] _board = new int[15, 15];

        private static bool _tryWrongPoint;
        private static bool _tryNotEmptyPoint;

        private static string _sendMessage;

        private static async Task Main(string[] args)
        {

            string message;
            GenerativeModel gemini = _googleAi.CreateGenerativeModel("models/gemini-2.5-flash-lite");
            gemini.SystemInstruction = _persona;

            ChatSession gomokuAI = gemini.StartChat();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("＠ⓐⓑⓒⓓⓔⓕⓖⓗⓘⓙⓚⓛⓜⓝⓞ");
                UpdateBoard();
                SystemMassage();

                string order = Console.ReadLine();

                if (TryPlayerTurn(order) == false) continue;

                if (CheckVictory())
                    return;

                _sendMessage = order;
                await AITurn(gomokuAI);

                if (CheckVictory())
                    return;
            }
        }

        private static void UpdateBoard()
        {
            for (int i = 0; i < _leftSide.Length; i++)
                UpdateLine(i);
        }

        private static void UpdateLine(int line)
        {
            Console.Write(_leftSide[line]);
            for (int i = 0; i < _leftSide.Length; i++)
            {
                if (_board[line, i] == 0)
                    Console.Write(_pointNull);

                else if (_board[line, i] == 1)
                    Console.Write(_pointWhite);

                else if (_board[line, i] == 2)
                    Console.Write(_pointBlack);
            }
            Console.WriteLine();
        }

        private static void SystemMassage()
        {
            if (_tryWrongPoint == true)
            {
                Console.WriteLine("X좌표는 a~o, Y좌표는 1~15로 해야합니다. ex) a13");
                _tryWrongPoint = false;
            }
            else if (_tryNotEmptyPoint == true)
            {
                Console.WriteLine("그 좌표엔 이미 돌이 있습니다.");
                _tryNotEmptyPoint = false;
            }
            else
                Console.WriteLine("어디어 놓을까요?");
        }

        private static bool TryPlayerTurn(string order)
        {
            if (CheckInputOrder(order) == false)
            {
                _tryWrongPoint = true;
                return false;
            }

            if (CheckPointStone(order) != 0)
            {
                _tryNotEmptyPoint = true;
                return false;
            }

            ExecuteOrder(order, 2);
            return true;
        }

        private static async Task AITurn(ChatSession ai)
        {
            while (true)
            {
                GenerateContentResponse response = await ai.GenerateContentAsync(_sendMessage);

                string order = response.Text.Trim();

                if (CheckInputOrder(order) == false)
                {
                    _sendMessage = "좌표";
                    continue;
                }
                
                int stone = CheckPointStone(order);

                if (stone != 0)
                {
                    _sendMessage = $"중복{stone}";
                    continue;
                }

                ExecuteOrder(order, 1);
                return;
            }
        }

        private static bool CheckInputOrder(string order)
        {
            if (order.Length != 3)
                return false;

            if ((order[0] >= 'a' && order[0] <= 'o') == false)
                return false;

            if (order[1] == '0' && order[2] == '0')
                return false;

            if ((order[1] == '0' || order[1] == '1') == false)
                return false;

            if ((order[2] >= '0' && order[2] <= '9') == false)
                return false;

            if ((order[1] == '1' && order[2] >= '6') == true)
                return false;

            return true;
        }

        private static int CheckPointStone(string order)
        {
            int pointX = (int)order[0] - 97;
            int pointY = (int.Parse(order.Substring(1))) - 1;

            return _board[pointY, pointX];
        }

        private static void ExecuteOrder(string order, int stone = 2)
        {
            int pointX = (int)order[0] - 97;
            int pointY = (int.Parse(order.Substring(1))) - 1;

            _board[pointY, pointX] = stone;
        }

        private static bool CheckVictory()
        {
            int size = _board.GetLength(0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int stone = _board[y, x];
                    if (stone != 0)
                    {
                        if (CheckLine(x, y, stone))
                        {
                            string winner = stone == 2 ? "플레이어(흑돌)" : "AI(백돌)";
                            Console.Clear();
                            UpdateBoard();
                            Console.WriteLine($"\n★★★ {winner}의 승리입니다! 오목 완성! ★★★");
                            Console.WriteLine("계속하려면 엔터를 누르세요...");
                            Console.ReadLine();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool CheckLine(int x, int y, int stone)
        {
            int[,] directions = new int[,] { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };
            int size = _board.GetLength(0);

            for (int i = 0; i < 4; i++)
            {
                int dx = directions[i, 0];
                int dy = directions[i, 1];
                int count = 1;

                for (int k = 1; k < 5; k++)
                {
                    int nextX = x + dx * k;
                    int nextY = y + dy * k;

                    if (nextX < 0 || nextX >= size || nextY < 0 || nextY >= size ||
                        _board[nextY, nextX] != stone)
                    {
                        break;
                    }

                    count++;
                }

                if (count >= 5)
                {
                    return true;
                }
            }

            return false;
        }
    }
}