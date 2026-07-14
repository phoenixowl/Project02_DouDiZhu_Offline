using System.Collections.Generic;

public class CardGroup
{
    private List<Card> cardList = new List<Card>();
    private CardType type = CardType.Invalid;

    // 核心修改：采用权重序列代替单一的主次权重
    private List<int> weightSequence = new List<int>();

    public List<Card> Cards => cardList;
    public CardType Type => type;
    public List<int> WeightSequence => weightSequence;

    public CardGroup(List<Card> inputCards)
    {
        CardRule.AnalyzeCardGroup(inputCards, this);
    }

    public void UpdateGroupInfo(List<Card> sortedCards, CardType cardType, List<int> weights)
    {
        this.cardList = sortedCards;
        this.type = cardType;
        this.weightSequence = weights;
    }
}