using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RPG
{
    public partial class MainWindow : Window
    {
        private Rectangle player;
        private Label playerLabel;
        private TextBlock playerEmoji;
        private double playerSpeed = 5;
        private bool gameStarted = false;
        private Random random = new Random();
        private List<Rectangle> obstacles = new List<Rectangle>();
        private TextBlock enemyMessage;
        private Button fightButton;
        private string selectedClass = "";
        private string playerName = "";
        private Dictionary<Rectangle, int> enemyHealth = new Dictionary<Rectangle, int>();
        private Dictionary<Rectangle, TextBlock> enemyHealthTexts = new Dictionary<Rectangle, TextBlock>();
        private Rectangle currentEnemy;
        private TextBlock playerHPText;
        private int playerHP;
        private bool isPlayerTurn = true;
        private bool canMove = true;
        private bool isEnemyCollision = false;
        private TextBlock logTextBlock;
        private ScrollViewer logScroll;

        public MainWindow()
        {
            InitializeComponent();
            this.ResizeMode = ResizeMode.NoResize;
            InitializeLog();
            LoadCharacterList();
            CreateBattleUI();
        }

        private void InitializeLog()
        {
            logTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = Brushes.Black
            };
            logScroll = new ScrollViewer
            {
                Content = logTextBlock,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Width = kiirasLabel.Width,
                Height = kiirasLabel.Height
            };
            kiirasLabel.Content = logScroll;
        }

        private void CreateBattleUI()
        {
            enemyMessage = new TextBlock
            {
                Text = "Harc Kezdete",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Green,
                Width = 150,
                Height = 40,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden
            };

            fightButton = new Button
            {
                Content = "Kezdés",
                Width = 80,
                Height = 35,
                Background = Brushes.DarkGreen,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Visibility = Visibility.Hidden
            };
            fightButton.Click += FightButton_Clicked;
            double startX = 10;
            double startY = 10;
            Canvas.SetLeft(enemyMessage, startX);
            Canvas.SetTop(enemyMessage, startY);
            Canvas.SetLeft(fightButton, startX + enemyMessage.Width + 10);
            Canvas.SetTop(fightButton, startY);

            gameCanvas.Children.Add(enemyMessage);
            gameCanvas.Children.Add(fightButton);
        }

        private void OnCharacterInputChanged(object sender, EventArgs e)
        {
            btnSaveCharacter.IsEnabled = !string.IsNullOrWhiteSpace(txtName.Text) && cmbClass.SelectedItem != null;
            if (cmbClass.SelectedItem is ComboBoxItem item)
            {
                selectedClass = item.Content.ToString();
                logTextBlock.Text = selectedClass switch
                {
                    "Fighter" => "Fighter:\n- Sebzés: 25-36\n- HP: 100",
                    "Archer" => "Archer:\n- Sebzés: 21-29\n- HP: 80",
                    "Lovas" => "Lovas:\n- Sebzés: 32-41\n- HP: 120",
                    _ => ""
                };
            }
        }

        private void SaveCharacter_Click(object sender, RoutedEventArgs e)
        {
            playerName = txtName.Text;
            selectedClass = (cmbClass.SelectedItem as ComboBoxItem)?.Content.ToString();
            File.AppendAllText("characters.txt", $"{playerName},{selectedClass}\n");
            MessageBox.Show("Karakter elmentve!");
            LoadCharacterList();
        }

        private void LoadCharacterList()
        {
            cmbLoadCharacter.Items.Clear();
            if (File.Exists("characters.txt"))
            {
                foreach (string line in File.ReadAllLines("characters.txt"))
                {
                    cmbLoadCharacter.Items.Add(line);
                }
            }
        }

        private void OnLoadCharacterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLoadCharacter.SelectedItem != null)
            {
                string[] data = cmbLoadCharacter.SelectedItem.ToString().Split(',');
                playerName = data[0];
                selectedClass = data[1];
                txtSelectedCharacter.Text = cmbLoadCharacter.SelectedItem.ToString();
                btnStart.IsEnabled = true;
                logTextBlock.Text = selectedClass switch
                {
                    "Fighter" => "Fighter:\n- Sebzés: 25-36\n- HP: 100",
                    "Archer" => "Archer:\n- Sebzés: 21-29\n- HP: 80",
                    "Lovas" => "Lovas:\n- Sebzés: 32-41\n- HP: 120",
                    _ => ""
                };
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            gameStarted = true;
            obstacles.Clear();
            enemyHealth.Clear();
            enemyHealthTexts.Clear();
            logTextBlock.Text = "";

            playerHP = selectedClass switch
            {
                "Fighter" => 100,
                "Archer" => 80,
                "Lovas" => 120,
                _ => 100
            };

            player = new Rectangle
            {
                Width = 25,
                Height = 25,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            playerLabel = new Label
            {
                Content = playerName,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };

            playerEmoji = new TextBlock
            {
                Text = GetClassEmoji(),
                FontSize = 18,
                FontFamily = new FontFamily("Segoe UI Emoji"),
                Width = 25,
                Height = 25,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            Canvas.SetLeft(player, 50);
            Canvas.SetTop(player, 200);
            Canvas.SetLeft(playerEmoji, 50);
            Canvas.SetTop(playerEmoji, 200);
            Canvas.SetLeft(playerLabel, 50);
            Canvas.SetTop(playerLabel, 175);
            playerHPText = new TextBlock
            {
                Text = $"{playerHP}❤️",
                FontSize = 14,
                Foreground = Brushes.White,
                Width = 50,
                Height = 20,
                TextAlignment = TextAlignment.Left
            };
            Canvas.SetLeft(playerHPText, 50);
            Canvas.SetTop(playerHPText, 225);

            gameCanvas.Children.Add(player);
            gameCanvas.Children.Add(playerEmoji);
            gameCanvas.Children.Add(playerLabel);
            gameCanvas.Children.Add(playerHPText);

            this.KeyDown += MainWindow_KeyDown;
            SpawnSquares(15);

            txtName.IsEnabled = false;
            cmbClass.IsEnabled = false;
            btnSaveCharacter.IsEnabled = false;
            cmbLoadCharacter.IsEnabled = false;
            btnStart.IsEnabled = false;
            txtSelectedCharacter.IsEnabled = false;
        }

        private string GetClassEmoji()
        {
            return selectedClass switch
            {
                "Fighter" => "⚔️",
                "Archer" => "🏹",
                "Lovas" => "🐎",
                _ => ""
            };
        }

        private void SpawnSquares(int count)
        {
            var starLevels = new List<int>();
            starLevels.AddRange(Enumerable.Repeat(1, 6));
            starLevels.AddRange(Enumerable.Repeat(2, 5));
            starLevels.AddRange(Enumerable.Repeat(3, 4));
            starLevels = starLevels.OrderBy(x => random.Next()).ToList();

            double labelX = Canvas.GetLeft(emptyLabel);
            double labelY = Canvas.GetTop(emptyLabel);
            double labelWidth = emptyLabel.Width;
            double labelHeight = emptyLabel.Height;

            for (int i = 0; i < count; i++)
            {
                Rectangle square;
                double posX, posY;
                bool collides;
                do
                {
                    posX = random.Next((int)labelX + 5, (int)(labelX + labelWidth - 30));
                    posY = random.Next((int)labelY + 5, (int)(labelY + labelHeight - 30));
                    collides = obstacles.Any(existing =>
                        Math.Abs(posX - Canvas.GetLeft(existing)) < 30 &&
                        Math.Abs(posY - Canvas.GetTop(existing)) < 30);
                } while (collides);

                square = new Rectangle
                {
                    Width = 25,
                    Height = 25,
                    Fill = Brushes.Red,
                    Tag = starLevels[i],
                    Visibility = Visibility.Hidden 
                };

                int hp = starLevels[i] switch
                {
                    1 => 100,
                    2 => 125,
                    3 => 150,
                    _ => 100
                };

                TextBlock hpText = new TextBlock
                {
                    Text = $"{hp}❤️",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    Width = 50,
                    Height = 20,
                    TextAlignment = TextAlignment.Center,
                    Background = Brushes.Black 
                };
                Panel.SetZIndex(hpText, 1);
                hpText.Visibility = Visibility.Hidden;

                Canvas.SetLeft(square, posX);
                Canvas.SetTop(square, posY);
                Canvas.SetLeft(hpText, posX);
                Canvas.SetTop(hpText, posY - 20);

                gameCanvas.Children.Add(square);
                gameCanvas.Children.Add(hpText);
                obstacles.Add(square);
                enemyHealth[square] = hp;
                enemyHealthTexts[square] = hpText;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!gameStarted || player == null || isEnemyCollision || !canMove)
                return;

            double left = Canvas.GetLeft(player);
            double top = Canvas.GetTop(player);
            double newX = left, newY = top;

            switch (e.Key)
            {
                case Key.Left: newX -= playerSpeed; break;
                case Key.Right: newX += playerSpeed; break;
                case Key.Up: newY -= playerSpeed; break;
                case Key.Down: newY += playerSpeed; break;
            }

            if (!CheckCollision(newX, newY))
            {
                Canvas.SetLeft(player, newX);
                Canvas.SetTop(player, newY);
                Canvas.SetLeft(playerEmoji, newX);
                Canvas.SetTop(playerEmoji, newY);
                Canvas.SetLeft(playerLabel, newX);
                Canvas.SetTop(playerLabel, newY - 25);
                Canvas.SetLeft(playerHPText, newX);
                Canvas.SetTop(playerHPText, newY + 25);
                AdjustPlayerLabelPosition();
            }
        }

        private void AdjustPlayerLabelPosition()
        {
            foreach (var enemyHP in enemyHealthTexts.Values)
            {
                double enemyHPLeft = Canvas.GetLeft(enemyHP);
                double enemyHPTop = Canvas.GetTop(enemyHP);
                double enemyHPWidth = enemyHP.Width;
                double enemyHPHeight = enemyHP.Height;

                double labelLeft = Canvas.GetLeft(playerLabel);
                double labelTop = Canvas.GetTop(playerLabel);
                double labelWidth = playerLabel.Width;
                double labelHeight = playerLabel.Height;

                Rect enemyRect = new Rect(enemyHPLeft, enemyHPTop, enemyHPWidth, enemyHPHeight);
                Rect labelRect = new Rect(labelLeft, labelTop, labelWidth, labelHeight);

                if (enemyRect.IntersectsWith(labelRect))
                {
                    Canvas.SetTop(playerLabel, enemyHPTop + enemyHPHeight + 5);
                }
            }
        }

        private bool CheckCollision(double newX, double newY)
        {
            double goalX = Canvas.GetLeft(goalLabel);
            double goalY = Canvas.GetTop(goalLabel);
            double goalWidth = goalLabel.Width;
            double goalHeight = goalLabel.Height;

            double playerRight = newX + 25;
            double playerLeft = newX;
            double playerTop = newY;
            double playerBottom = newY + 25;

            bool hitGoal = playerRight >= goalX && playerLeft < goalX + goalWidth &&
                           playerBottom > goalY && playerTop < goalY + goalHeight;
            if (hitGoal)
            {
                ShowWinScreen();
                return true;
            }

            if (newX < 10 || newX + 25 > 590 || newY < 60 || newY > 325)
                return true;

            foreach (Rectangle enemy in obstacles)
            {
                double rectX = Canvas.GetLeft(enemy);
                double rectY = Canvas.GetTop(enemy);
                if (newX < rectX + 25 && newX + 25 > rectX &&
                    newY < rectY + 25 && newY + 25 > rectY)
                {
                    currentEnemy = enemy;
                    enemy.Visibility = Visibility.Visible;
                    enemyHealthTexts[enemy].Visibility = Visibility.Visible;
                    ShowBattleUI();
                    isEnemyCollision = true;
                    return true;
                }
            }
            return false;
        }

        private void ShowBattleUI()
        {
            enemyMessage.Visibility = Visibility.Visible;
            fightButton.Visibility = Visibility.Visible;
        }

        private async void FightButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (currentEnemy == null)
                return;

            MessageBox.Show("Harc kezdete!");
            enemyMessage.Visibility = Visibility.Hidden;
            fightButton.Visibility = Visibility.Hidden;
            canMove = false;
            logTextBlock.Text = "";
            isPlayerTurn = random.Next(0, 2) == 0;

            while (playerHP > 0 && enemyHealth[currentEnemy] > 0)
            {
                if (isPlayerTurn)
                {
                    int damage = selectedClass switch
                    {
                        "Fighter" => random.Next(25, 36),
                        "Archer" => random.Next(21, 29),
                        "Lovas" => random.Next(32, 41),
                        _ => 20
                    };

                    enemyHealth[currentEnemy] -= damage;
                    enemyHealthTexts[currentEnemy].Text = $"{enemyHealth[currentEnemy]}❤️";
                    logTextBlock.Text += $"{playerName} {damage} sebzést adott az ellenségnek!\n";
                    isPlayerTurn = false;
                }
                else
                {
                    int star = (int)currentEnemy.Tag;
                    int enemyDmg = star switch
                    {
                        1 => random.Next(15, 21),
                        2 => random.Next(25, 31),
                        3 => random.Next(35, 41),
                        _ => 15
                    };

                    playerHP -= enemyDmg;
                    playerHPText.Text = $"{playerHP}❤️";
                    logTextBlock.Text += $"Az ellenség {enemyDmg} sebzést adott {playerName}-nek!\n";
                    isPlayerTurn = true;
                }

                if (enemyHealth[currentEnemy] <= 0)
                {
                    RemoveEnemy(currentEnemy);
                    playerHP += 50;
                    playerHPText.Text = $"{playerHP}❤️";
                    logTextBlock.Text += $"{playerName} +50 HP-t kapott!\n";
                    break;
                }

                if (playerHP <= 0)
                {
                    foreach (Rectangle enemy in obstacles)
                    {
                        enemy.Visibility = Visibility.Visible;
                        enemyHealthTexts[enemy].Visibility = Visibility.Visible;
                    }
                    ShowLossScreen();
                    return;
                }

                logScroll.ScrollToEnd();
                await Task.Delay(3000);
            }

            if (playerHP > 0)
            {
                logTextBlock.Text += "Az ellenség vesztett!\n";
                logScroll.ScrollToEnd();
                await Task.Delay(2000);
                logTextBlock.Text = "";
            }
            isEnemyCollision = false;
            canMove = true;
        }

        private void RemoveEnemy(Rectangle enemy)
        {
            gameCanvas.Children.Remove(enemy);
            gameCanvas.Children.Remove(enemyHealthTexts[enemy]);
            obstacles.Remove(enemy);
            enemyHealth.Remove(enemy);
            enemyHealthTexts.Remove(enemy);
            enemyMessage.Visibility = Visibility.Hidden;
            fightButton.Visibility = Visibility.Hidden;
            isEnemyCollision = false;
        }

        private void ShowWinScreen()
        {
            foreach (Rectangle enemy in obstacles)
            {
                enemy.Visibility = Visibility.Visible;
                enemyHealthTexts[enemy].Visibility = Visibility.Visible;
            }

            canMove = false;
            double canvasWidth = gameCanvas.ActualWidth;
            double startY = 20;
            Label winLabel = new Label
            {
                Content = "Nyertél!",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Green,
                Width = 120,
                Height = 40,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Button restartButton = new Button
            {
                Content = "Új játék!",
                Width = 80,
                Height = 35,
                Background = Brushes.DarkGreen,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            restartButton.Click += RestartGame;
            double totalWidth = winLabel.Width + restartButton.Width + 10;
            double startX = (canvasWidth - totalWidth) / 2;
            Canvas.SetLeft(winLabel, startX);
            Canvas.SetTop(winLabel, startY);
            Canvas.SetLeft(restartButton, startX + winLabel.Width + 10);
            Canvas.SetTop(restartButton, startY);
            gameCanvas.Children.Add(winLabel);
            gameCanvas.Children.Add(restartButton);
        }

        private void ShowLossScreen()
        {
            canMove = false;
            double canvasWidth = gameCanvas.ActualWidth;
            double startY = 20;
            Label lossLabel = new Label
            {
                Content = "Vesztettél!",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Red,
                Width = 120,
                Height = 40,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Button restartButton = new Button
            {
                Content = "Új játék!",
                Width = 80,
                Height = 35,
                Background = Brushes.DarkGreen,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            restartButton.Click += RestartGame;
            double totalWidth = lossLabel.Width + restartButton.Width + 10;
            double startX = (canvasWidth - totalWidth) / 2;
            Canvas.SetLeft(lossLabel, startX);
            Canvas.SetTop(lossLabel, startY);
            Canvas.SetLeft(restartButton, startX + lossLabel.Width + 10);
            Canvas.SetTop(restartButton, startY);
            gameCanvas.Children.Add(lossLabel);
            gameCanvas.Children.Add(restartButton);
        }

        private void RestartGame(object sender, RoutedEventArgs e)
        {
            txtName.IsEnabled = true;
            cmbClass.IsEnabled = true;
            btnSaveCharacter.IsEnabled = true;
            cmbLoadCharacter.IsEnabled = true;
            txtSelectedCharacter.IsEnabled = true;
            btnStart.IsEnabled = cmbLoadCharacter.SelectedItem != null;

            List<UIElement> elementsToRemove = new List<UIElement>();
            foreach (UIElement element in gameCanvas.Children)
            {
                if (element != goalLabel && !(element is Rectangle &&
                    (Canvas.GetLeft(element) == 0 || Canvas.GetTop(element) == 50 ||
                     Canvas.GetTop(element) == 350 || Canvas.GetLeft(element) == 590)))
                {
                    elementsToRemove.Add(element);
                }
            }
            foreach (UIElement element in elementsToRemove)
            {
                gameCanvas.Children.Remove(element);
            }
            CreateBattleUI();
            enemyHealth.Clear();
            enemyHealthTexts.Clear();
            logTextBlock.Text = "";
            gameStarted = false;
            canMove = true;
        }

        public void Quit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Biztosan ki akarsz lépni?", "Kilépés", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (File.Exists("characters.txt"))
                    {
                        File.WriteAllText("characters.txt", "");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hiba: {ex.Message}");
                }
                Application.Current.Shutdown();
            }
        }
    }
}
