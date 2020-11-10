-- SPDX-License-Identifier: MIT
-- Copyright (c) 2020 oO (https://github.com/oocytanb)

local settings = {
    enableDebugging = false,
    queueCapacity = 128,
    intervalTime = TimeSpan.FromMilliseconds(1000),
    pastCommentTime = TimeSpan.FromMinutes(10),
    maxAvatarListLength = 32
}

---@class commentMessageEntry
---@field message string
---@field name string
---@field commentSource string

-- luacheck: globals commentMessageQueue

if vci.assets.IsMine then
    ---@type cytanb @See `cytanb_annotations.lua`
    cytanb = cytanb or require('cytanb')(_ENV)

    commentMessageQueue = commentMessageQueue or cytanb.CreateCircularQueue(settings.queueCapacity)

    local timestampThreshold = cytanb.UnixTime() - settings.pastCommentTime.TotalSeconds

    local vciLoaded = false

    cytanb.SetOutputLogLevelEnabled(true)
    if settings.enableDebugging then
        cytanb.SetLogLevel(cytanb.LogLevelAll)
    end

    local lastSentTime = TimeSpan.Zero

    -- アバターの一覧をダンプする
    local dumpAvatarList = function ()
        local avatarList = vci.studio.GetAvatars()
        if avatarList then
            for i = 1, settings.maxAvatarListLength do
                local ava = avatarList[i]
                if not ava then
                    break
                end
                local id = ava and ava.GetId() or ''
                local name = ava and ava.GetName() or ''
                cytanb.LogInfo(name, ' : ', id)
            end
        end
    end

    local UpdateCw; UpdateCw = cytanb.CreateUpdateRoutine(
        function (deltaTime, unscaledDeltaTime)
            ---@type commentMessageEntry
            local now = vci.me.UnscaledTime
            if now >= lastSentTime + settings.intervalTime then
                local entry = commentMessageQueue.Poll()
                if entry and (entry.timestamp and entry.timestamp >= timestampThreshold) then
                    lastSentTime = now
                    cytanb.EmitCommentMessage(entry.message or '', {
                        name = entry.name,
                        commentSource = entry.commentSource,
                    })
                end
            end
        end,

        function ()
            cytanb.LogTrace('OnLoad')
            vciLoaded = true

            -- アバターリストをダンプする
            dumpAvatarList()

            vci.message.On('notification', function (sender, name, message)
                if message == 'joined' or message == 'left' then
                    dumpAvatarList()
                end
            end)
        end,

        function (reason)
            cytanb.LogError('Error on update routine: ', reason)
            UpdateCw = function () end
        end
    )

    update = function ()
        UpdateCw()
    end
end

-- if commentMessageQueue then
--     commentMessageQueue.Offer({message = 'Dummy Message', name = 'DummyUser', commentSource = 'DummySource', timestamp = 123456789})
-- end
