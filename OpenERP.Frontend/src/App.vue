<script setup>
import { reactive, ref } from 'vue'
import { Calendar, Lock, OfficeBuilding, User } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'

const form = reactive({
  fiscalYear: '2024年度账',
  company: '宏易科技有限公司',
  account: '',
  password: ''
})

const language = reactive({
  value: 'zh-CN'
})

const isSubmitting = ref(false)

const yearOptions = ['2024年度账', '2025年度账', '2026年度账']
const companyOptions = ['宏易科技有限公司', '宏易科技集团总部', '宏易科技华东分公司']

const login = async () => {
  if (!form.account || !form.password) {
    ElMessage.warning('请输入账号和密码')
    return
  }

  isSubmitting.value = true

  try {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        fiscalYear: form.fiscalYear,
        company: form.company,
        account: form.account,
        password: form.password
      })
    })

    const payload = await response.json()

    if (!response.ok || !payload?.success) {
      ElMessage.error(payload?.message ?? '登录失败，请检查账号或密码。')
      return
    }

    ElMessage.success(`欢迎，${payload.displayName ?? form.account}`)
    window.location.href = payload.redirectUrl ?? '/'
  } catch (error) {
    ElMessage.error('网络异常，暂时无法登录。')
    console.error(error)
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="login-scene">
    <div class="top-actions">
      <el-select v-model="language.value" class="lang-select" size="small">
        <el-option label="简体中文" value="zh-CN" />
        <el-option label="English" value="en-US" />
      </el-select>
    </div>

    <div class="bg-layer">
      <div class="blob b1"></div>
      <div class="blob b2"></div>
      <div class="blob b3"></div>
      <div class="noise"></div>
    </div>

    <main class="login-shell">
      <section class="brand-side">
        <h1>宏易科技有限公司</h1>
        <div class="brand-logo">
          <span>f</span>
        </div>
      </section>

      <section class="form-side">
        <el-form :model="form" label-position="top" class="login-form" @submit.prevent="login">
          <el-form-item>
            <el-select v-model="form.fiscalYear" class="field">
              <template #prefix>
                <el-icon><Calendar /></el-icon>
              </template>
              <el-option
                v-for="item in yearOptions"
                :key="item"
                :label="item"
                :value="item"
              />
            </el-select>
          </el-form-item>

          <el-form-item>
            <el-select v-model="form.company" class="field">
              <template #prefix>
                <el-icon><OfficeBuilding /></el-icon>
              </template>
              <el-option
                v-for="item in companyOptions"
                :key="item"
                :label="item"
                :value="item"
              />
            </el-select>
          </el-form-item>

          <el-form-item>
            <el-input v-model="form.account" class="field" placeholder="请输入账号">
              <template #prefix>
                <el-icon><User /></el-icon>
              </template>
            </el-input>
          </el-form-item>

          <el-form-item>
            <el-input
              v-model="form.password"
              class="field"
              type="password"
              placeholder="请输入密码"
              show-password
            >
              <template #prefix>
                <el-icon><Lock /></el-icon>
              </template>
            </el-input>
          </el-form-item>

          <el-button class="submit-btn" type="primary" :loading="isSubmitting" @click="login">
            登 录
          </el-button>

          <a class="forgot-link" href="javascript:void(0)">忘记密码</a>
        </el-form>
      </section>
    </main>
  </div>
</template>
