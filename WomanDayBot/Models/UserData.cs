﻿namespace WomanDayBot.Models
{
  /// <summary>
  /// Class for storing persistent user data.
  /// </summary>
  public class UserData
  {
    public string Name { get; set; }
    public string Room { get; set; }
    public bool DidBotWelcomeUser { get; set; }
  }
}
