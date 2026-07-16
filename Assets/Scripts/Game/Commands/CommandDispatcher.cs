using System;

namespace DouDiZhu.Logic.Commands
{
    //已经弃用
    /// <summary>
    /// 命令调度器（全局静态类）
    /// </summary>
   /*public static class CommandDispatcher
    {
        /// <summary>
        /// 拦截器委托：返回 true 表示命令已被拦截（联机模式），无需本地执行
        /// </summary>
        public static Func<ICommand, bool> Interceptor { get; set; }

        /// <summary>
        /// 发送并执行命令
        /// </summary>
        public static void Send(ICommand command)
        {
            // 1. 如果有拦截器且拦截器返回 true，则命令由外部（网络层）处理
            if (Interceptor != null && Interceptor.Invoke(command))
            {
                // 联机版：命令已序列化发送给服务器，本地不执行
                return;
            }

            // 2. 单机版（或未拦截）：直接本地执行
            command.Execute();
        }
    }*/
}