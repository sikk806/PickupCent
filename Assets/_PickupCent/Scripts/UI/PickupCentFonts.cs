using UnityEngine;

namespace PickupCent.UI
{
    /// <summary>
    /// 스타일 가이드 4장이 요구하는 "둥근 느낌의 제목/숫자용 폰트"와 "깔끔한 본문용 폰트" 2종 분류를
    /// 코드 한 곳에서 관리한다. 프로젝트에 TextMeshPro나 별도 .ttf/.otf 폰트 파일이 전혀 없어서
    /// (Editor/폴더 전수 조사로 확인) 실제로는 Unity 내장 레거시 폰트(LegacyRuntime/Arial) 하나만
    /// 재사용하고, Bold(제목/숫자) vs Normal(본문) 굵기 차이로만 두 카테고리를 구분한다 — 이는 폰트
    /// 파일을 새로 추가하기 전까지의 임시 대체이며, 최종 보고 시 사용자에게 그대로 알린다.
    /// </summary>
    public static class PickupCentFonts
    {
        private static Font cached;

        /// <summary>본문(라벨/설명 등)에 쓰는 폰트.</summary>
        public static Font Default
        {
            get
            {
                if (cached == null)
                {
                    cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (cached == null) cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return cached;
            }
        }

        /// <summary>제목/숫자(둥근 느낌 강조)에 쓰는 폰트 — 현재는 Default와 동일 폰트, Bold로만 구분.</summary>
        public static Font Title => Default;
    }
}
