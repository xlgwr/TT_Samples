﻿/*
    Copyright © 2018-2019 Trading Technologies International, Inc. All Rights Reserved Worldwide

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this
    list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
    this list of conditions and the following disclaimer in the documentation
    and/or other materials provided with the distribution.

    * Redistributions of source or binary code must be free of charge.

    * Neither the name of the copyright holder nor the names of its
    contributors may be used to endorse or promote products derived from
    this software without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
    AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
    FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
    SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
    OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
    OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FillDownload
{
    class FillDownloadThread
    {
        Thread m_thread;
        Boolean m_running = false;
        TimeSpan m_startTime = default(TimeSpan);
        TimeSpan m_endTime = default(TimeSpan);
        TimeSpan m_interval = default(TimeSpan);
        DateTime m_startDate = default(DateTime);
        DateTime m_minTimeStamp = default(DateTime);
        bool[] m_daysToRun;
        private static readonly int max_timeout_retries = 16;
        private static readonly int max_narrowing_retries = 32;

        object m_lock = new object();

        public FillDownloadThread(TimeSpan start_time, TimeSpan end_time, TimeSpan interval, bool[] days_to_run)
            :this(start_time, end_time, interval, days_to_run, DateTime.Today)
        {
        }

        public FillDownloadThread(TimeSpan start_time, TimeSpan end_time, TimeSpan interval, bool[] days_to_run, DateTime start_date)
        {
            m_startTime = start_time;
            m_endTime = end_time;
            m_interval = interval;
            m_startDate = start_date;
            m_daysToRun = days_to_run;
            m_minTimeStamp = m_startDate;
        }

        public void Start()
        {
            try
            { 
                CommonEnums.BuildEnums();
            }
            catch (Exception e)
            {
                RaiseErrorEvent("Error: " + e.Message + e.StackTrace);
                return;
            }

            m_thread = new Thread(this.ThreadMain);
            m_thread.Name = "Fill Download Thread";
            m_running = true;
            m_thread.Start();
        }

        void ThreadMain()
        {
            while (m_running)
            {
                ThreadWait(GetNextDownloadPeriod());

                while (m_running && DateTime.Now < DateTime.Today + m_endTime)
                {
                    try
                    {
                        DownloadFills();
                    }
                    catch(Exception e)
                    {
                        RaiseErrorEvent("Error: " + e.Message + e.StackTrace);
                        m_running = false;
                        break;  
                    }

                    ThreadWait(m_interval);
                }
            }
        }

        private void DownloadFills()
        {
            // Perform REST request/response to download fill data, specifying our cached minimum timestamp as a starting point.  
            // On a successful response the timestamp will be updated so we run no risk of downloading duplicate fills.

            bool should_continue = false;
            List<TT_Fill> fills = new List<TT_Fill>();
            do
            {
                should_continue = false;

                var min_param = new RestSharp.Parameter("minTimestamp", TT_Info.ToRestTimestamp(m_minTimeStamp).ToString(), RestSharp.ParameterType.QueryString);

                RestSharp.IRestResponse result = RestManager.GetRequest("ttledger", "fills", max_timeout_retries, min_param);

                Type t = result.GetType();
                bool isDict = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);
                if (isDict == false &&  result.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                {
                    should_continue = true;
                    DateTime max_time = DateTime.Now.ToUniversalTime();

                    int retry_count = 0;
                    for(retry_count = 0; retry_count < max_narrowing_retries; ++retry_count)
                    {
                        DateTime mid_time = m_minTimeStamp + TimeSpan.FromTicks((max_time - m_minTimeStamp).Ticks / 2);
                        FDLog.LogMessage("Fill request timed out. Retrying....");
                        FDLog.LogMessage(String.Format("min_time:{0} ({1}), max_time: {2} ({3}), mid_time: {4} ({5})", m_minTimeStamp.ToString("yyyy/MM/dd HH:mm:ss.ffff"), TT_Info.ToRestTimestamp(m_minTimeStamp).ToString(), max_time.ToString("yyyy/MM/dd HH:mm:ss.ffff"), TT_Info.ToRestTimestamp(max_time).ToString(), mid_time.ToString("yyyy/MM/dd HH:mm:ss.ffff"), TT_Info.ToRestTimestamp(mid_time).ToString()));
                        max_time = mid_time;//m_minTimeStamp + TimeSpan.FromTicks((max_time - m_minTimeStamp).Ticks / 2);
                        var max_param = new RestSharp.Parameter("maxTimestamp", TT_Info.ToRestTimestamp(max_time).ToString(), RestSharp.ParameterType.QueryString);

                        result = RestManager.GetRequest("ttledger", "fills", max_timeout_retries, min_param, max_param);

                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            m_minTimeStamp = max_time;
                            break;
                        }
                        else if (result.StatusCode != System.Net.HttpStatusCode.GatewayTimeout)
                        {
                            throw new Exception(String.Format("Request for fills unsuccessful. (minTimestamp={0}) - Status: {1} - Error Message: {2}", min_param.Value.ToString(), result.StatusCode.ToString(), result.ErrorMessage));
                        }

                        if(retry_count == max_narrowing_retries)
                        {
                            throw new Exception("Request for fills unsuccessful. Max Retries exceeded.");
                        }
                    }
                }
                else if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception(String.Format("Request for fills unsuccessful. (minTimestamp={0}) - Status: {1} - Error Message: {2}", min_param.Value.ToString(), result.StatusCode.ToString(), result.ErrorMessage));
                }

                JObject json_data = JObject.Parse(result.Content);
                FDLog.LogMessage(String.Format("Downloaded {0} fills.", json_data["fills"].Count()));
                foreach (var fill in json_data["fills"])
                {
                    fills.Add(new TT_Fill(fill));
                }

                fills.Sort((f1, f2) => f1.UtcTimeStamp.CompareTo(f2.UtcTimeStamp));
                if (fills.Count > 0)
                    m_minTimeStamp = new DateTime(fills[fills.Count - 1].UtcTimeStamp.Ticks + 1);
                RaiseFillDownloadEvent(fills);

                should_continue |= (fills.Count == TT_Info.MAX_RESPONSE_FILLS);
                should_continue &= m_running;
            }
            while (should_continue);
        }


        public delegate void FillDownloadEventHandler(object sender, List<TT_Fill> fills);
        public event FillDownloadEventHandler FillDownload;
        private void RaiseFillDownloadEvent(List<TT_Fill> fills)
        {
            if (FillDownload != null)
                FillDownload(this, fills);
        }

        public delegate void DownloadThreadErrorHandler(object sender, string error_message);
        public event DownloadThreadErrorHandler OnError;
        private void RaiseErrorEvent(string error_message)
        {
            if (OnError != null)
                OnError(this, error_message);
        }

        TimeSpan GetNextDownloadPeriod()
        {
            // Return the time from now until the next scheduled download period 
            // or now if we are already in one.

            int day_of_week = (int)DateTime.Today.DayOfWeek;
            int next_day = (day_of_week + 1) % 7;

            if(m_daysToRun[day_of_week] == true && DateTime.Now.TimeOfDay > m_startTime && DateTime.Now.TimeOfDay < m_endTime)
            {
                return default(TimeSpan);
            }
            else
            {
                int days_until;

                if(m_daysToRun[day_of_week] == true && DateTime.Now.TimeOfDay < m_startTime)
                {
                    days_until = 0;
                }
                else
                {

                    for (; next_day != day_of_week; next_day = (next_day + 1) % 7)
                    {
                        if (m_daysToRun[next_day] == true)
                            break;
                    }

                    days_until = ((next_day - day_of_week + 7) % 7);

                    if(days_until == 0)
                        days_until = 7;
                }

                DateTime nextStart = DateTime.Today.Date.AddDays(days_until) + m_startTime;
                return nextStart - DateTime.Now;
            }
        }

        public void StopDownloading()
        {
            m_running = false;
        }

        public void StopThread()
        {
            if (m_thread != null)
            {
                m_running = false;
                m_interval = m_interval = TimeSpan.MinValue;
                ThreadNotify();
                m_thread.Join();
            }
        }

        private void ThreadWait(TimeSpan interval)
        {
            lock(m_lock)
            {
                if(m_running)
                {
                    Monitor.Wait(m_lock, interval);
                }
            }
        }

        private void ThreadNotify()
        {
            lock(m_lock)
            {
                Monitor.Pulse(m_lock);
            }
        }
    }
}