package io.enbug.billing.google;

import android.content.Context;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.Looper;

import androidx.annotation.NonNull;

import java.lang.ref.WeakReference;

class Helper {
    @NonNull
    private static final HandlerThread backgroundThread;
    @NonNull
    private static final Handler backgroundHandler;
    @NonNull
    private static final Handler uiHandler;

    private static boolean initialized;
    private static WeakReference<Context> context;

    static {
        backgroundThread = new HandlerThread("enbug-billing");
        backgroundThread.start();

        uiHandler = new Handler(Looper.getMainLooper());
        backgroundHandler = new Handler(backgroundThread.getLooper());
    }

    static void initialize(@NonNull Context context) {
        if (initialized) return;

        Context applicationContext = context.getApplicationContext();
        if (applicationContext != null) {
            context = applicationContext;
        }

        Helper.context = new WeakReference<>(context);

        initialized = true;
    }

    @NonNull
    static Context getContext() {
        return context.get();
    }

    @NonNull
    static Handler getUiHandler() {
        return uiHandler;
    }

    @NonNull
    static Handler getBackgroundHandler() {
        return backgroundHandler;
    }

    private Helper() {

    }
}
