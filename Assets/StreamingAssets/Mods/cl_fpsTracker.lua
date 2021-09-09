

function OnInit ()
	console.log("fps tracker loaded :D")
end

local frameCount = 0.0
local timer = 0.0
local sampleTime = 0.5
local frameRate = 0

function OnUpdate(deltaTime)

	frameCount = frameCount + 1.0
	timer = timer + deltaTime

	if (timer >= sampleTime) then
		frameRate = frameCount / sampleTime

		timer = 0.0
		frameCount = 0
	end

end

function OnGUI()
	gui.label(frameRate)
	if(gui.button("Hello World")) then
		console.log("button pressed")
	end
end
