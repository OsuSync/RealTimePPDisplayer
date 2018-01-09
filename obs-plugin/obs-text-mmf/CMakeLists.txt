if (NOT WIN32)
	return()
endif()

project(obs-text-mmf)

if(WIN32)
	set(obs-text-mmf_PLATFORM_SOURCES
		gdiplus/obs-text.cpp)
	set(obs-text-mmf_PLATFORM_DEPS
		gdiplus)
endif()

add_library(obs-text-mmf MODULE
	${obs-text-mmf_PLATFORM_SOURCES}
	${obs-text-mmf_PLATFORM_HEADERS})
target_link_libraries(obs-text-mmf
	libobs
	${obs-text-mmf_PLATFORM_DEPS})

install_obs_plugin_with_data(obs-text-mmf data)
