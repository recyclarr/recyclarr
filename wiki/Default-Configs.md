Streaming Optimized 



```radarr:
# Set the URL/API Key to your actual instance
  - base_url: http://192.168.1.X:7878/radarr
    api_key:
    quality_definition:
      type: movie
    # Custom Format Settings
    delete_old_custom_formats: false
    custom_formats:
      - trash_ids:
#HQ Source Groups
          - 26fa26253af4001701fedb56cec376dc # HQ-WEBDL
#Misc
          - e7718d7a3ce595f289bfee26adc178f5 # Repack/Proper
          - 0d91270a7255a1e388fa85e959f359d8 # FreeLeech (use if you want to prefer FreeLeech in a tie breaker situation)
          - 4a3b087eea2ce012fcc1ce319259a3be #Anime Dual Audio (If using anime in a hybrid instance tags should be used)
#Streaming Services
          - b3b3a6ac74ecbd56bcdbefa4799fb9df #Amazon
          - 3472d276482257d68f7836a55ca24877 #Apple TV+
          - 84272245b2988854bfb76a16e60baea5 #Disney+
          - 5763d1b0ce84aff3b21038eea8e9b8ad #HBO Max
          - 526d445d4c16214309f0fd2b3be18a89 #Hulu
          - 170b1d363bd8516fbf3a3eb05d4faff6 #Netflix
          - e36a0ba1bc902b26ee40818a1d59b8bd #Paramount+
          - c9fd353f8f5f1baf56dc601c4cb29920 #Peacock TV
#unwanted
          - b8cd450cbfa689c0259a01d9e29ba3d6 # 3D
          - ed38b889b31be83fda192888e2286d83 # BR-DISK
          - 90a6f9a284dff5103f6346090e6280c8 # LQ
          - ae9b7c9ebde1f3bd336a8cbd1ec4c5e5 # No-RlsGroup
          - 7357cf5161efbf8c4d5d0c30b4815ee2 # Obfuscated
          - 90cedc1fea7ea5d11298bebd3d1d3223 # EVO (no WEBDL)
          - 923b6abef9b17f937fab56cfcf89e1f1 # DV (WEBDL)
          - b2be17d608fc88818940cd1833b0b24c # x265 (720/1080p)

        quality_profiles:
          - name: Any
            reset_unmatched_scores: true
      - trash_ids:
          - 1c7d7b04b15cc53ea61204bebbcc1ee2 # HQ 0
          - 403f3f6266b90439cacc1e07cae4dc2d # HQ-Remux 0```
