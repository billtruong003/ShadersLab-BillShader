using UnityEngine;

namespace Utilities.Timers
{
    /// <summary>
    /// Một utility đơn giản để giới hạn tần suất của một hành động.
    /// Nó không cần chạy trong Update() và hoạt động dựa trên logic timestamp.
    /// </summary>
    public sealed class TimeGate
    {
        private readonly float _interval;
        private float _lastPassTime;

        /// <summary>
        /// Khởi tạo một TimeGate với khoảng thời gian chờ.
        /// </summary>
        /// <param name="interval">Thời gian (giây) cần chờ trước khi cổng mở lại.</param>
        public TimeGate(float interval)
        {
            _interval = interval;
            // Đặt thời gian lần cuối đi qua là một giá trị âm
            // để đảm bảo lần kiểm tra đầu tiên luôn thành công.
            _lastPassTime = -interval;
        }

        /// <summary>
        /// Kiểm tra xem cổng đã sẵn sàng để đi qua chưa.
        /// Nếu có, nó sẽ tự động cập nhật lại mốc thời gian và trả về true.
        /// </summary>
        /// <returns>True nếu đã đủ thời gian chờ, ngược lại là false.</returns>
        public bool TryPass()
        {
            if (Time.time >= _lastPassTime + _interval)
            {
                _lastPassTime = Time.time;
                return true;
            }

            return false;
        }
    }
}