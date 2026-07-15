namespace DouDiZhu.Logic.Models
{
    public enum CardType
    {
        Invalid = 0,        // 非法牌型
        Single,             // 单张
        Pair,               // 对子
        Triple,             // 三张不带
        TripleWithOne,      // 三带一
        TripleWithTwo,      // 三带二 (一对)
        Straight,           // 顺子 (5张及以上)
        DoubleStraight,     // 连对 (3对及以上)
        Airplane,           // 飞机 (多组连续三张)
        AirplaneWithSingle, // 飞机带等量单
        AirplaneWithPair,   // 飞机带等量对
        FourWithTwo,        // 四带二
        Bomb,               // 炸弹
        KingBomb            // 王炸 (双王)
    }
}