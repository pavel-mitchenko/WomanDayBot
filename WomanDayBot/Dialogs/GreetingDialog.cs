﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using WomanDayBot.Models;

namespace WomanDayBot.Dialogs
{
  /// <summary>Defines a dialog for collecting a user's name.</summary>
  public class GreetingsDialog : DialogSet
  {
    /// <summary>The ID of the main dialog.</summary>
    public const string MainDialog = "main";

    private const string NamePromt = "namePromt";
    private const string RoomPromt = "roomPromt";

    // Define keys for tracked values within the dialog.
    private const string Name = "name";
    private const string Room = "room";

    /// <summary>Creates a new instance of this dialog set.</summary>
    /// <param name="dialogState">The dialog state property accessor to use for dialog state.</param>
    public GreetingsDialog(IStatePropertyAccessor<DialogState> dialogState)
      : base(dialogState)
    {
      var steps = new WaterfallStep[]
      {
        PromtForNameAsync,
        PromtForRoomAsync,
        AcknowledgeUserDataAsync
      };

      Add(new TextPrompt(NamePromt, this.UserNamePromptValidatorAsync));
      Add(new ChoicePrompt(RoomPromt));
      Add(new WaterfallDialog(MainDialog, steps));
    }

    private async Task<DialogTurnResult> PromtForNameAsync(
      WaterfallStepContext stepContext,
      CancellationToken cancellationToken = default(CancellationToken))
    {
      var message = stepContext.Context.Activity;
      if (message.Type == ActivityTypes.ConversationUpdate)
      {
        foreach (var member in message.MembersAdded ?? Array.Empty<ChannelAccount>())
        {
          if (member.Id == message.Recipient.Id)
          {
            // Prompt for the party size. The result of the prompt is returned to the next step of the waterfall.
            return await stepContext.PromptAsync(
              NamePromt,
              new PromptOptions
              {
                Prompt = MessageFactory.Text("Не то, чтобы я хотел подкатить, но как тебя зовут. Принцесса?"),
                RetryPrompt = MessageFactory.Text("Да ладно, ну скажи имечко?")
              },
              cancellationToken);
          }
        }
      }
      return null;
    }

    /// <summary>
    /// User name validator
    /// </summary>
    /// <param name="promptContext">String that need to validate</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if name valid and false if not valid</returns>
    private async Task<bool> UserNamePromptValidatorAsync(
      PromptValidatorContext<string> promptContext,
      CancellationToken cancellationToken = default(CancellationToken))
    {
      if (!promptContext.Recognized.Succeeded)
      {
        await promptContext.Context.SendActivityAsync(
          "Извините, но я вас не понял. Пожалуйста, введите своё имя.",
          cancellationToken: cancellationToken);

        return false;
      }

      var value = promptContext.Recognized.Value;

      var regex = new Regex(@"(\w)+");

      if (value != null && regex.IsMatch(value))
      {
        return true;
      }

      await promptContext.Context.SendActivitiesAsync(new[]
      {
        MessageFactory.Text("К сожалению, я не могу распознать ваше имя."),
        promptContext.Options.RetryPrompt
      },
      cancellationToken);

      return false;
    }

    private async Task<DialogTurnResult> PromtForRoomAsync(
      WaterfallStepContext stepContext,
      CancellationToken cancellationToken = default(CancellationToken))
    {
      // Record the name information in the current dialog state.
      var name = (string)stepContext.Result;
      stepContext.Values[Name] = name;

      // Prompt for the party size. The result of the prompt is returned to the next step of the waterfall.
      return await stepContext.PromptAsync(
        RoomPromt,
        new PromptOptions
        {
          Prompt = MessageFactory.Text("Мы уже почти на одной волне. Черкани адресок: я заеду."),
          RetryPrompt = MessageFactory.Text("Да не домашний адрес. В офисе комнату напиши."),
          Choices = ChoiceFactory.ToChoices(new List<string> { "701", "702", "801", "802", "803", "806", "807", "808" })
        },
        cancellationToken);
    }

    private async Task<DialogTurnResult> AcknowledgeUserDataAsync(
      WaterfallStepContext stepContext,
      CancellationToken cancellationToken = default(CancellationToken))
    {
      // Record the party size information in the current dialog state.
      var room = (stepContext.Result as FoundChoice).Value;
      stepContext.Values[Room] = room;

      // Send an acknowledgement to the user.
      await stepContext.Context.SendActivityAsync("Ну теперь-то мы с тобой зажжем!", cancellationToken: cancellationToken);

      // Return the collected information to the parent context.
      var userData = new UserData
      {
        Name = (string)stepContext.Values[Name],
        Room = (string)stepContext.Values[Room]
      };

      return await stepContext.EndDialogAsync(userData, cancellationToken);
    }
  }
}
