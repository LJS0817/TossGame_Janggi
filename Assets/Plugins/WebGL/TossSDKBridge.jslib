mergeInto(LibraryManager.library, {

    /**
     * 토스 햅틱 진동 피드백 트리거
     * @param typePtr ("light" | "medium" | "heavy" | "success" | "warning" | "error")
     */
    Toss_TriggerHaptic: function (typePtr) {
        var type = UTF8ToString(typePtr);
        try {
            if (window.toss && window.toss.hapticFeedback) {
                window.toss.hapticFeedback(type);
            } else if (navigator.vibrate) {
                if (type === "light") navigator.vibrate(15);
                else if (type === "heavy" || type === "warning") navigator.vibrate([30, 50, 30]);
                else if (type === "success") navigator.vibrate([20, 30, 60]);
                else navigator.vibrate(25);
            } else {
                console.log("[TossSDK WebGL] Haptic trigger:", type);
            }
        } catch (e) {
            console.warn("[TossSDK WebGL] Haptic error:", e);
        }
    },

    /**
     * 토스 친구 공유 / 외부 공유 시트 호출
     * @param titlePtr 공유 제목
     * @param descPtr 공유 설명 문구
     */
    Toss_ShareResult: function (titlePtr, descPtr) {
        var title = UTF8ToString(titlePtr);
        var desc = UTF8ToString(descPtr);
        try {
            if (window.toss && window.toss.share) {
                window.toss.share({
                    title: title,
                    text: desc
                });
            } else if (navigator.share) {
                navigator.share({
                    title: title,
                    text: desc
                }).catch(function (err) {
                    console.log("[TossSDK WebGL] Share cancelled or failed:", err);
                });
            } else {
                console.log("[TossSDK WebGL] Share called:", title, desc);
                alert("[" + title + "]\n" + desc);
            }
        } catch (e) {
            console.warn("[TossSDK WebGL] Share error:", e);
        }
    },

    /**
     * 토스 보상형 광고 호출
     * @param callbackObjPtr 응답을 받을 유니티 GameObject 이름
     * @param callbackMethodPtr 응답 메소드 이름 ("1" 성공, "0" 실패/취소)
     */
    Toss_ShowRewardedAd: function (callbackObjPtr, callbackMethodPtr) {
        var callbackObj = UTF8ToString(callbackObjPtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);

        try {
            if (window.toss && window.toss.showRewardedAd) {
                window.toss.showRewardedAd({
                    onSuccess: function () {
                        SendMessage(callbackObj, callbackMethod, "1");
                    },
                    onFail: function () {
                        SendMessage(callbackObj, callbackMethod, "0");
                    }
                });
            } else {
                console.log("[TossSDK WebGL] Simulated Ad watching (3s)...");
                setTimeout(function () {
                    // 브라우저/로컬 환경에서는 가상으로 광고 시청 성공 콜백 전달
                    if (confirm("[토스 광고 시뮬레이션]\n광고 시청을 완료하고 보상을 받으시겠습니까?")) {
                        SendMessage(callbackObj, callbackMethod, "1");
                    } else {
                        SendMessage(callbackObj, callbackMethod, "0");
                    }
                }, 300);
            }
        } catch (e) {
            console.warn("[TossSDK WebGL] Rewarded Ad error:", e);
            SendMessage(callbackObj, callbackMethod, "0");
        }
    },

    /**
     * 토스 웹뷰 닫기 (앱 내 나가기)
     */
    Toss_CloseWebview: function () {
        try {
            if (window.toss && window.toss.close) {
                window.toss.close();
            } else {
                console.log("[TossSDK WebGL] Close webview called.");
            }
        } catch (e) {
            console.warn("[TossSDK WebGL] Close error:", e);
        }
    }

});
