package com.smartmicrogrid.operator

import android.content.Context
import android.content.res.ColorStateList
import android.graphics.Color
import android.graphics.drawable.GradientDrawable
import android.view.Gravity
import android.view.View
import android.widget.*
import androidx.annotation.DrawableRes
import com.google.android.material.card.MaterialCardView
import com.google.android.material.color.MaterialColors
import com.google.android.material.imageview.ShapeableImageView
import com.smartmicrogrid.R

internal object OperatorVisuals {
    val green = Color.rgb(16, 185, 129)
    val cyan = Color.rgb(6, 182, 212)
    val amber = Color.rgb(245, 158, 11)
    val blue = Color.rgb(59, 130, 246)
    val purple = Color.rgb(99, 102, 241)
    val red = Color.rgb(239, 68, 68)

    fun panel(context: Context, padding: Int = 16): Pair<MaterialCardView, LinearLayout> {
        val body = context.column().apply { setPadding(context.dp(padding), context.dp(padding), context.dp(padding), context.dp(padding)) }
        val card = MaterialCardView(context).apply {
            radius = context.dp(18).toFloat(); cardElevation = 0f
            strokeWidth = context.dp(1); strokeColor = context.getColor(R.color.surface_outline)
            setCardBackgroundColor(MaterialColors.getColor(this, com.google.android.material.R.attr.colorSurface))
            addView(body)
        }
        return card to body
    }

    fun photo(context: Context, title: String, subtitle: String, @DrawableRes image: Int, height: Int = 116, compact: Boolean = false): MaterialCardView {
        return MaterialCardView(context).apply {
            radius = context.dp(18).toFloat(); cardElevation = 0f; strokeWidth = 0
            addView(FrameLayout(context).apply {
                minimumHeight = context.dp(height)
                addView(object : ImageView(context) {
                    // Decorative photos fill the text-sized banner; they never size it themselves.
                    override fun onMeasure(widthMeasureSpec: Int, heightMeasureSpec: Int) {
                        setMeasuredDimension(MeasureSpec.getSize(widthMeasureSpec),
                            if (MeasureSpec.getMode(heightMeasureSpec) == MeasureSpec.EXACTLY) MeasureSpec.getSize(heightMeasureSpec) else 0)
                    }
                }.apply {
                    setImageResource(image); scaleType = ImageView.ScaleType.CENTER_CROP
                    importantForAccessibility = View.IMPORTANT_FOR_ACCESSIBILITY_NO
                }, FrameLayout.LayoutParams(-1, -1))
                addView(View(context).apply { setBackgroundResource(if (compact) R.drawable.bg_operator_welcome_overlay else R.drawable.bg_auth_photo_overlay) }, FrameLayout.LayoutParams(-1, -1))
                addView(context.column().apply {
                    minimumHeight = context.dp(height); gravity = Gravity.BOTTOM
                    val padding = context.dp(if (compact) 12 else 16)
                    setPadding(padding, padding, padding, padding)
                    addView(context.label(title, if (compact) 18f else 22f, true).apply { setTextColor(Color.WHITE); androidx.core.view.ViewCompat.setAccessibilityHeading(this, true) })
                    if (subtitle.isNotBlank()) addView(context.label(subtitle, if (compact) 12f else 13f).apply { setTextColor(0xFFE2EEFC.toInt()); setPadding(0, context.dp(4), 0, 0) })
                }, FrameLayout.LayoutParams(-1, -2))
            }, FrameLayout.LayoutParams(-1, -2))
        }
    }

    fun thumbnail(context: Context, @DrawableRes image: Int, size: Int = 52) = ShapeableImageView(context).apply {
        setImageResource(image); scaleType = ImageView.ScaleType.CENTER_CROP
        shapeAppearanceModel = shapeAppearanceModel.toBuilder().setAllCornerSizes(context.dp(12).toFloat()).build()
        importantForAccessibility = View.IMPORTANT_FOR_ACCESSIBILITY_NO
        layoutParams = LinearLayout.LayoutParams(context.dp(size), context.dp(size)).apply { marginEnd = context.dp(12) }
    }

    fun pill(context: Context, title: String, warning: Boolean = false): TextView = context.label(title.replace(Regex("([a-z])([A-Z])"), "$1 $2"), 11f, true).apply {
        val foreground = context.getColor(if (warning) R.color.m3_status_warning_foreground else R.color.m3_status_info_foreground)
        setTextColor(foreground)
        background = GradientDrawable().apply { setColor(context.getColor(if (warning) R.color.m3_status_warning_background else R.color.m3_status_info_background)); cornerRadius = context.dp(20).toFloat() }
        setPadding(context.dp(9), context.dp(5), context.dp(9), context.dp(5))
    }

    fun stat(context: Context, title: String, value: Double, suffix: String, detail: String, accent: Int, @DrawableRes icon: Int): MaterialCardView {
        val (card, body) = panel(context, 10)
        val header = LinearLayout(context).apply { gravity = Gravity.CENTER_VERTICAL }
        header.addView(ImageView(context).apply {
            setImageResource(icon); imageTintList = ColorStateList.valueOf(accent)
            importantForAccessibility = View.IMPORTANT_FOR_ACCESSIBILITY_NO
        }, LinearLayout.LayoutParams(context.dp(18), context.dp(18)).apply { marginEnd = context.dp(6) })
        header.addView(context.label(title, 12f), LinearLayout.LayoutParams(0, -2, 1f))
        body.addView(header)
        body.addView(context.label("", 21f, true).apply { setPadding(0, context.dp(5), 0, 0); OperatorMotion.count(this, value, suffix) })
        body.addView(context.label(detail, 11f).apply { setPadding(0, context.dp(3), 0, 0) })
        return card
    }
}
