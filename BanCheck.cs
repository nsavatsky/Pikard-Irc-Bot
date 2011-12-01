using System;

namespace PikardIrcBot
{
    /// <summary>
    /// Runs every second and checks if a banned user is due for unbanning
    /// </summary>
    internal class BanCheck : PeriodicTask
    {
        protected override void Run()
        {
            if (Pikard.OpPrivileges)
            {
                try
                {
                    //Loops through all the banned users
                    TimeSpan result;
                    for (int i = 0; i < Pikard.BannedUsers.Count; i++)
                    {
                        //takes the result of Now and their banned timestamp
                        result = DateTime.Now - Pikard.BannedUsers[i].BanTime;

                        //if result is larger than the designated ban duration, unban
                        if (result >= Pikard.BannedUsers[i].BanDuration)
                        {
                            Pikard.Unban(Pikard.BannedUsers[i].NickName);
                            //Remove the user from the list
                            Pikard.BannedUsers.RemoveAt(i);

                            //save the list to disk
                            Pikard.SerializeToXml(Pikard.BannedUsers);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}