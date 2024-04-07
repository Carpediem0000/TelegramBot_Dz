using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http;
using Newtonsoft.Json;
using TelegramBot_Dz;

var botClient = new TelegramBotClient("7173090461:AAFqQHmlp2-HA8Tp9trKquYwejs-B4VSI_E");
const string apiWeather = "bcb395514086595b809ab3007e0d819a";

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
   updateHandler: HandleUpdateAsync,
   pollingErrorHandler: HandlePollingErrorAsync,
   receiverOptions: receiverOptions,
   cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;

    var chatId = message.Chat.Id;


    
    var keyboard = new ReplyKeyboardMarkup(new[]
        {
            KeyboardButton.WithRequestLocation("Узнать погоду"),
        });
    if (message.Text == "/start")
    {
        await botClient.SendTextMessageAsync(chatId, message.Text, replyMarkup: keyboard);
    }
    if (message.Type == Telegram.Bot.Types.Enums.MessageType.Location)
    {
        var latitude = message.Location.Latitude;
        var longitude = message.Location.Longitude;

        var res = await GetWeatherAsync(latitude, longitude);
        await botClient.SendTextMessageAsync(chatId, res);
    }
}

static async Task<string> GetWeatherAsync(double latitude, double longitude)
{
    var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude.ToString("f2")}&lon={longitude.ToString("f2")}&lang=ru&exclude=current&appid={apiWeather}&units=metric";

    HttpClient httpClient = new HttpClient();
    var response = await httpClient.GetAsync(apiUrl);

    if (response.IsSuccessStatusCode)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var weatherResponse = JsonConvert.DeserializeObject<WeatherResponse>(responseContent);

        // Извлекаем нужные данные из объекта
        string res = $"Город: {weatherResponse.name}\n" +
            $"Температура: {weatherResponse.main.temp} °C\n" +
            $"Описание погоды: {weatherResponse.weather[0].description}\n" +
            $"Скорость ветра: {weatherResponse.wind.speed} м/с";
        return res;
    }
    else
    {
        return "Не удалось получить информацию о погоде.";
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}
