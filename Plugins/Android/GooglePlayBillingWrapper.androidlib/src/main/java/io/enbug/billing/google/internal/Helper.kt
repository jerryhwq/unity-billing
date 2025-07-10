package io.enbug.billing.google.internal

import android.content.Context
import android.os.Handler
import android.os.HandlerThread
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.android.asCoroutineDispatcher

internal object Helper {
    private val backgroundThread by lazy {
        val ret = HandlerThread("enbug-billing")
        ret.start()
        ret
    }

    internal val backgroundHandler by lazy {
        Handler(backgroundThread.looper)
    }

    private val backgroundDispatcher by lazy {
        backgroundHandler.asCoroutineDispatcher()
    }

    internal val backgroundScope by lazy {
        CoroutineScope(backgroundDispatcher + SupervisorJob())
    }

    private var initialized = false

    internal lateinit var applicationContext: Context
        private set

    internal fun initialize(context: Context) {
        if (initialized) return

        applicationContext = context.applicationContext ?: context

        initialized = true
    }
}