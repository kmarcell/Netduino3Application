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
        public event SendFrameEventHandler SendFrame;

        private Queue waitingForResponseQueue;
        private Queue waitingForSendingQueue;

        public FrameQueueService()
        {
            waitingForResponseQueue = new Queue();
            waitingForSendingQueue = new Queue();
        }

        public void EnqueueFrame(Frame frame, Callback callback)
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
                ResponseReceived(frame);
            }
        }

        public void ResponseReceived(Frame response)
        {
            int index = waitingForResponseQueue.IndexOfFramePassingTest(delegate(Frame frame) {
                return (frame.FrameID == response.FrameID) && isResponseForRequest(response, frame);
            });

            if (index == NOTFOUND) { return; }

            Callback callback = waitingForResponseQueue.CallbackForFrameAtIndex(index);
            if (callback != null && callback(response))
            {
                waitingForResponseQueue.RemoveAt(index);
            }
        }

        private bool isResponseForRequest(Frame response, Frame request)
        {
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
