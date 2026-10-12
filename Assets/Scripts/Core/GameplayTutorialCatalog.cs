using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class GameplayTutorialPage
    {
        public GameplayTutorialPage(
            string id,
            string chineseTitle,
            string englishTitle,
            string chineseBody,
            string englishBody,
            string chineseAction,
            string englishAction)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Page id is required.", nameof(id));
            Id = id;
            ChineseTitle = chineseTitle ?? string.Empty;
            EnglishTitle = englishTitle ?? string.Empty;
            ChineseBody = chineseBody ?? string.Empty;
            EnglishBody = englishBody ?? string.Empty;
            ChineseAction = chineseAction ?? string.Empty;
            EnglishAction = englishAction ?? string.Empty;
        }

        public string Id { get; }
        public string ChineseTitle { get; }
        public string EnglishTitle { get; }
        public string ChineseBody { get; }
        public string EnglishBody { get; }
        public string ChineseAction { get; }
        public string EnglishAction { get; }
    }

    /// <summary>
    /// Stable tutorial copy. Visual assets remain art-owned; this catalog only
    /// supplies runtime text and semantic page identifiers.
    /// </summary>
    public static class GameplayTutorialCatalog
    {
        private static readonly GameplayTutorialPage[] PagesInternal =
        {
            new GameplayTutorialPage(
                "wallet-login",
                "钱包登录",
                "WALLET LOGIN",
                "1. 连接钱包，进入游戏",
                "1. Connect your wallet and enter the game",
                "完成游戏登录",
                "Complete game sign-in"),
            new GameplayTutorialPage(
                "starter-pack",
                "领取新手卡包",
                "CLAIM STARTER PACK",
                "1. 领取角色卡包\n2. 在卡组页面查看、确认获得的角色",
                "1. Claim the character pack\n2. Review the received characters on the Deck page",
                "获得 3 个随机普通品质角色",
                "Receive 3 random Common characters"),
            new GameplayTutorialPage(
                "edit-deck",
                "编辑卡组",
                "EDIT DECK",
                "1. 进入卡组页\n2. 点击编辑卡组\n3. 将 3 名角色加入生效卡组\n4. 保存编辑卡组",
                "1. Open Deck\n2. Tap Edit Deck\n3. Add 3 characters to the active deck\n4. Save the deck",
                "上场 3 个角色",
                "Deploy 3 characters"),
            new GameplayTutorialPage(
                "start-expedition",
                "开始 D0 远征",
                "START D0 EXPEDITION",
                "1. 返回主页，点击开始远征\n2. 选择 D0 难度\n3. 确认卡组并开始航行",
                "1. Return Home and tap Start Expedition\n2. Select D0\n3. Confirm the deck and set sail",
                "抵达路线终点 BOSS 关卡",
                "Reach the final Boss node"),
            new GameplayTutorialPage(
                "defeat-boss",
                "击败首领",
                "DEFEAT THE BOSS",
                "1. 运用策略击败 BOSS",
                "1. Use your strategy to defeat the Boss",
                "战胜 BOSS",
                "Defeat the Boss"),
            new GameplayTutorialPage(
                "claim-reward",
                "签名领取奖励",
                "SIGN AND CLAIM REWARDS",
                "1. 核对结算掉落物\n2. 签名确认并领取",
                "1. Review settlement drops\n2. Sign to confirm and claim",
                "相应掉落物到账",
                "Receive the listed drops")
        };

        public static IReadOnlyList<GameplayTutorialPage> Pages => PagesInternal;

        public static GameplayTutorialPage GetPage(int index)
        {
            if (index < 0 || index >= PagesInternal.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return PagesInternal[index];
        }
    }
}
