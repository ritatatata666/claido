<template>
  <div class="auth-page">
    <div class="auth-card">
      <h1>Create Account</h1>
      <p>Create an account so your case history is saved.</p>
      <form @submit.prevent="submit">
        <label>
          Username
          <input v-model.trim="username" autocomplete="username" required />
        </label>
        <label>
          Password
          <input v-model="password" type="password" autocomplete="new-password" required />
        </label>
        <label>
          Confirm password
          <input v-model="confirmPassword" type="password" autocomplete="new-password" required />
        </label>
        <button class="btn-primary" :disabled="loading">{{ loading ? 'Creating...' : 'Create account' }}</button>
      </form>
      <p v-if="error" class="auth-error">{{ error }}</p>
      <p class="auth-link">Already have an account? <RouterLink to="/login">Sign in</RouterLink></p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useAuthStore } from '../stores/authStore.js'

const router = useRouter()
const auth = useAuthStore()
const username = ref('')
const password = ref('')
const confirmPassword = ref('')
const loading = ref(false)
const error = ref('')

async function submit() {
  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match.'
    return
  }

  loading.value = true
  error.value = ''
  try {
    await auth.register(username.value, password.value)
    router.push('/')
  } catch (e) {
    error.value = e.message || 'Registration failed.'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 24px;
}

.auth-card {
  width: min(420px, 100%);
  padding: 24px;
  border-radius: 12px;
  border: 1px solid var(--border-color);
  background: rgba(255, 255, 255, 0.96);
  box-shadow: 0 14px 40px rgba(0, 0, 0, 0.15);
}

h1 {
  margin: 0 0 10px;
}

p {
  margin: 0 0 18px;
  color: var(--text-secondary);
}

form {
  display: grid;
  gap: 12px;
}

label {
  display: grid;
  gap: 6px;
  font-size: 14px;
}

input {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  font-size: 14px;
}

button {
  margin-top: 8px;
}

.auth-error {
  margin-top: 12px;
  color: var(--accent-red);
}

.auth-link {
  margin-top: 10px;
}
</style>
