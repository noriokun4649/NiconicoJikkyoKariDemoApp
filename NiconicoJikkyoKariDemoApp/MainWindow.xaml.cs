using System.Collections.ObjectModel;
using System.Windows;

namespace NiconicoJikkyoKariDemoApp
{
    public class CommentItem
    {
        public DateTime Time { get; set; }
        public string Comment { get; set; }
        public string ChannelName { get; set; }
        public string UserId { get; set; }
        public int Vpos { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ChatService chatService;
        private readonly ObservableCollection<CommentItem> comments = [];

        public MainWindow()
        {
            InitializeComponent();
            ChannelLists.ItemsSource = ChatServiceUrlGenerator.GetChannelList();
            CommentListView.ItemsSource = comments;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var channel = ChannelLists.Text;
            try
            {
                var uri = await ChatServiceUrlGenerator.GetChatDataAsync(channel);
                chatService = new ChatService(uri);
                chatService.MessageReceived += msg =>
                {
                    if (msg.Message != null)
                    {
                        var comment = new CommentItem()
                        {
                            ChannelName = channel,
                            Comment =  msg.Message.Chat.Content,
                            UserId = msg.Message.Chat.HashedUserId,
                            Time = DateTime.Now,
                            Vpos = msg.Message.Chat.Vpos
                        };
                        comments.Add(comment);
                        CommentListView.ScrollIntoView(comment);
                    }
                };
                await chatService.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            chatService.Dispose();
        }
    }
}