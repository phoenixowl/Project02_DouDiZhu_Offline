using System;
using System.Collections.Generic;

namespace DouDiZhu.Logic.Commands
{
    /// <summary>
    /// 玩家ID与座位索引映射器（单机/联机共用）
    /// 核心作用：让 RoundController 只认座位索引(0,1,2)，而命令持有全局PlayerID。
    /// </summary>
    public static class PlayerIdMapper
    {
        private static readonly Dictionary<int, int> _idToSeatMap = new Dictionary<int, int>();

        /// <summary>
        /// 注册映射关系（联机版由服务器根据房间信息注册，单机版注册 0->0）
        /// </summary>
        public static void Register(int playerId, int seatIndex)
        {
            if (_idToSeatMap.ContainsKey(playerId))
            {
                // 联机版通常不会重复注册，单机版为了避免重复调用可忽略或覆盖
                _idToSeatMap[playerId] = seatIndex;
            }
            else
            {
                _idToSeatMap.Add(playerId, seatIndex);
            }
        }

        /// <summary>
        /// 清空映射（游戏结束时调用，或房间销毁时调用）
        /// </summary>
        public static void Clear()
        {
            _idToSeatMap.Clear();
        }

        /// <summary>
        /// 根据PlayerID获取座位索引
        /// </summary>
        /// <exception cref="KeyNotFoundException">如果ID未注册，抛出异常（防止作弊或逻辑错误）</exception>
        public static int GetSeatIndex(int playerId)
        {
            if (_idToSeatMap.TryGetValue(playerId, out int seatIndex))
            {
                return seatIndex;
            }

            // 在联机版中，如果收到未注册的ID，说明是恶意请求或逻辑bug，抛出明确异常便于定位
            throw new KeyNotFoundException($"玩家ID {playerId} 未注册映射，无法转换为座位索引。请检查是否调用了 Register()。");
        }

        /// <summary>
        /// 检查ID是否有效（用于服务器端的安全校验）
        /// </summary>
        public static bool IsValidPlayer(int playerId)
        {
            return _idToSeatMap.ContainsKey(playerId);
        }
    }
}