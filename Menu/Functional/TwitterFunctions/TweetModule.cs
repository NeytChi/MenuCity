using System;
using Tweetinvi;
using YonderMedia.DB;
using Newtonsoft.Json;
using Tweetinvi.Models;
using System.Threading;
using System.Collections.Generic;
using YonderMedia.Functional.TwitterFuctional;

namespace Yonder.Functional.TwitterFuctional
{
    public class TweetModule
    {
        private LogProgram logger;
        private DatabaseYonder database;
        private string OAuthKey = "255407962-APrhVF2f17r04iPYfDFutxyhv1Zrq8CkDkpsIG0Z";
        private string OAuthSecret = "RwZzBPBDhDNQpWZwv7Ympf29RPKfQbsa94rrrd3X63cHH";
        private string OAuthConsumerKey = "Q5XYWHuyisdiFtBa2cLjHURV1";
        private string OAuthConsumerSecret = "iR8Y5P9O9Z5Zqst6c0W1j6s2XDDquDVCQLGNt8GVBrraU4OKBb";

        public List<TweetM> RecentTweets;
        public List<string> ScreenNames = new List<string>();

        public TweetModule(DatabaseYonder data_base, LogProgram logProgram)
        {
            this.logger = logProgram;
            this.database = data_base;
            RecentTweets = database.SelectTweets();
            ScreenNames = database.SelectScreenNames();
        }
        public string SelectJsonTweets(int start, int end)
        {
            List<TweetM> selected = new List<TweetM>();
            for (int i = RecentTweets.Count; i > RecentTweets.Count - end ;i--)
            {
                if (i == 0)
                {
                    break;
                }
                else
                {
                    selected.Add(RecentTweets[i - 1]);
                }
            }
            return "{\r\n" +
                   "\"success\":true,\r\n" +
                   "\"message\":\"Getting recent tweets\",\r\n" +
                   "\"tweets\":\r\n" +
                   JsonConvert.SerializeObject(selected) + "\r\n" +
            "}";
        }
        public void HandleTweets()
        {
            foreach(string screenName in ScreenNames)
            {
                List<TweetM> NewTweets = GetTweets(screenName);
                if (NewTweets != null)
                {
                    AddNewTweets(NewTweets);
                    UpdateTweetsDatabase(NewTweets);
                }
            }
            /*foreach(TweetM tweetM in RecentTweets)
            {
                Console.Write(tweetM.Tweet_Text + "- -"); 
                Console.Write(tweetM.Tweet_Favorite);
            }*/
            logger.WriteLog("Handle timeline twitter", LogLevel.Tweets);
        }
        public bool AddTwitterScreenNames(string screenName)
        {
            if (ScreenNames.Contains(screenName))
            {
                logger.WriteLog("Can not add screen_name=" + screenName + " .This screen_name exist", LogLevel.Tweets);
                return false;
            }
            ScreenNames.Add(screenName);
            database.AddScreenName(screenName);
            Thread thread = new Thread(HandleTweets);
            thread.IsBackground = true;
            thread.Start();
            logger.WriteLog("Add new screen name. Server will get tweets from them. ScreenName=" + screenName, LogLevel.Tweets);
            return true;
        }
        public bool DeleteScreenName(string screenName)
        {
            if(!ScreenNames.Contains(screenName))
            {
                logger.WriteLog("Can not delete screen_name=" + screenName + " .This screen_name not exist", LogLevel.Tweets);
                return false;
            }
            ScreenNames.Remove(screenName);
            database.DeleteScreenName(screenName);
            logger.WriteLog("Delete screen_name=" + screenName, LogLevel.Tweets);
            return true;
        }
        public List<TweetM> GetTweets(string ScreenName)
        {
            List<TweetM> answer = new List<TweetM>();
            Auth.SetUserCredentials(OAuthConsumerKey, OAuthConsumerSecret, OAuthKey, OAuthSecret);
            var user = User.GetUserFromScreenName(ScreenName);
            if (user == null)
            {
                logger.WriteLog("Can not get user details from ScreenName=" + ScreenName, LogLevel.Error);
                return answer;
            }
            var userTweets = Timeline.GetUserTimeline(ScreenName);
            if (userTweets == null)
            {
                logger.WriteLog("Can not get user timeline from ScreenName=" + ScreenName, LogLevel.Error);
                return answer;
            }
            if (userTweets != null)
            {
                foreach (ITweet tweet in userTweets)
                {
                    TweetM tweetM = new TweetM();
                    tweetM.Tweet_TwitterId = tweet.Id;
                    tweetM.Tweet_UserName = user.Name;
                    tweetM.Tweet_UserScreenName = user.ScreenName;
                    tweetM.Tweet_Text = tweet.Text;
                    tweetM.Tweet_Favorite = tweet.FavoriteCount;
                    tweetM.Tweet_Retweet = tweet.RetweetCount;
                    tweetM.Tweet_Url = tweet.Url;
                    tweetM.Create_at = (int)(tweet.CreatedAt.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
                    answer.Add(tweetM);
                }
            }
            userTweets = null;
            logger.WriteLog("Get tweets from account screenname=" + ScreenName, LogLevel.Tweets);
            return answer;
        }
        public void AddNewTweets(List<TweetM> new_tweets)
        {
            bool check = false;
            if (RecentTweets == null || RecentTweets.Count == 0)
            {
                foreach (TweetM tweet in new_tweets)
                {
                    database.AddTweet(tweet);
                    RecentTweets.Add(tweet);
                }
            }
            else
            {
                foreach (TweetM newTweet in new_tweets)
                {
                    check = false;
                    foreach (TweetM lastTweet in RecentTweets)
                    {
                        if (newTweet.Tweet_TwitterId == lastTweet.Tweet_TwitterId)
                        {
                            check = true;
                            break;
                        }
                    }
                    if (check == false)
                    {
                        database.AddTweet(newTweet);
                        RecentTweets.Add(newTweet);
                    }
                }
            }
            logger.WriteLog("Add list of tweets to database", LogLevel.Tweets);
        }
        public void UpdateTweetsDatabase(List<TweetM> new_tweets)
        {
            if (RecentTweets == null || RecentTweets.Count == 0)
            {
                return;
            }
            else
            {
                foreach (TweetM newTweet in new_tweets)
                {
                    foreach (TweetM lastTweet in RecentTweets)
                    {
                        if (newTweet.Tweet_TwitterId == lastTweet.Tweet_TwitterId)
                        {
                            if (newTweet.Tweet_Favorite != lastTweet.Tweet_Favorite || newTweet.Tweet_Retweet != lastTweet.Tweet_Retweet)
                            {
                                lastTweet.Tweet_Favorite = newTweet.Tweet_Favorite;
                                lastTweet.Tweet_Retweet = newTweet.Tweet_Retweet;
                                database.UpdateTweet(lastTweet);
                            }
                        }
                    }
                }
            }
            logger.WriteLog("Update list of tweets to database", LogLevel.Tweets);
        }
    }
}
