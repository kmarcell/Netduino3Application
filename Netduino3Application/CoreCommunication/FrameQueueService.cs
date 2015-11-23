using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    public delegate void SendFrameEventHandler(Frame frame);
    // Returns YES, if expected response arrived, NO otherwise.
    public delegate bool Callback(Frame response);

    class FrameQueueService
    {
        public static int NOTFOUND = -1;
        private static int MaxWaitingForResponseNumber = 32;
        public event SendFrameEventHandler SendFrame;

        private FrameQueue waitingForResponseQueue;
        private FrameQueue waitingForSendingQueue;

        public FrameQueueService()
        {
            waitingForResponseQueue = new FrameQueue();
            waitingForSendingQueue = new FrameQueue();
        }

        public void EnqueueFrame(ATCommandFrame frame, Callback callback)
        {
            int frameID = UnusedFrameId;
            if (frameID == NOTFOUND)
            {
                waitingForSendingQueue.Enqueue(frame, callback);
                return;
            }

            if (callback != null)
            {
                frame.FrameID = (byte)frameID;
                waitingForResponseQueue.Enqueue(frame, callback);

                if (waitingForResponseQueue.Count > MaxWaitingForResponseNumber)
                {
                    Callback kickOutcallback = waitingForResponseQueue.CallbackForFrameAtIndex(0);
                    if (kickOutcallback != null)
                    {
                        kickOutcallback(null);
                    }
                    waitingForResponseQueue.RemoveAt(0);
                }
            }
            else
            {
                frame.FrameID = 0;
            }
            
            SendFrame(frame);
        }

        public void onReceivedRemoteFrame(object sender, Frame frame)
        {
            if (frame is RemoteATCommandResponseFrame || frame is ATCommandResponseFrame)
            {
                ResponseReceived(frame as ATCommandFrame);
            }
        }

        public void ResponseReceived(ATCommandFrame response)
        {
            int index = waitingForResponseQueue.IndexOfFramePassingTest(delegate(Frame frame) {
                return (frame.FrameID == response.FrameID) && isResponseForRequest(response, (frame as ATCommandFrame));
            });

            if (index == NOTFOUND) { return; }

            Callback callback = waitingForResponseQueue.CallbackForFrameAtIndex(index);
            if (callback != null && callback(response))
            {
                waitingForResponseQueue.RemoveAt(index);
            }
        }

        private bool isResponseForRequest(ATCommandFrame response, ATCommandFrame request)
        {
            if (request.ATCommandName != response.ATCommandName)
            {
                return false;
            }

            if (request is RemoteATCommandRequestFrame)
            {
                return (Frame.isEqualAddress((request as RemoteATCommandRequestFrame).DestinationAddress16Bit, new byte[] { 0xFF, 0xFE }) &&
                    Frame.isEqualAddress((request as RemoteATCommandRequestFrame).DestinationAddress64Bit, (response as RemoteATCommandResponseFrame).SourceAddress64Bit)) ||
                    Frame.isEqualAddress((request as RemoteATCommandRequestFrame).DestinationAddress16Bit, (response as RemoteATCommandResponseFrame).SourceAddress16Bit);
            }

            if (request is ATCommandRequestFrame)
            {
                return true;
            }

            return false;
}

        public int UnusedFrameId
        {
            get
            {
                if (waitingForResponseQueue.Count == 255)
                {
                    return NOTFOUND;
                }

                for (int frameId = 1; frameId < 256; ++frameId)
                {
                    int index = waitingForResponseQueue.IndexOfFramePassingTest(delegate(Frame frame) { return frameId == frame.FrameID; });
                    if (index == NOTFOUND) { return frameId; }
                }

                return NOTFOUND;
            }
        }
    }
}
