public enum GameState
{
    Idle,       // Đang chờ lệnh (người chơi có thể bấm phím)
    Executing,  // Đang chạy lệnh (nhân vật đang di chuyển)
    Win,        // Đã thắng
    Dead        // Đã chết
}